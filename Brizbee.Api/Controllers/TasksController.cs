//
//  TasksController.cs
//  BRIZBEE API
//
//  Copyright (C) 2019-2021 East Coast Technology Services, LLC
//
//  This file is part of the BRIZBEE API.
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Affero General Public License as
//  published by the Free Software Foundation, either version 3 of the
//  License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Affero General Public License for more details.
//
//  You should have received a copy of the GNU Affero General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
//

using Brizbee.Api.Services;
using Brizbee.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;

namespace Brizbee.Api.Controllers
{
    public class TasksController : ODataController
    {
        private readonly IConfiguration _configuration;
        private readonly SqlContext _context;

        public TasksController(IConfiguration configuration, SqlContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        // GET: odata/Tasks
        [EnableQuery(PageSize = 1000, MaxExpansionDepth = 3)]
        public IQueryable<Brizbee.Core.Models.Task> GetTasks()
        {
            var currentUser = CurrentUser();

            return _context.Tasks
                .Include(t => t.Job.Customer)
                .Where(t => t.Job.Customer.OrganizationId == currentUser.OrganizationId);
        }

        // GET: odata/Tasks(5)
        [EnableQuery(MaxExpansionDepth = 3)]
        public SingleResult<Brizbee.Core.Models.Task> GetTask ([FromODataUri] int key)
        {
            var currentUser = CurrentUser();

            return SingleResult.Create(_context.Tasks
                .Include(t => t.Job.Customer)
                .Where(t => t.Job.Customer.OrganizationId == currentUser.OrganizationId)
                .Where(t => t.Id == key));
        }

        // POST: odata/Tasks
        public IActionResult Post([FromBody] Brizbee.Core.Models.Task task)
        {
            var currentUser = CurrentUser();

            var job = _context.Jobs
                .Include(j => j.Customer)
                .Where(j => j.Id == task.JobId)
                .FirstOrDefault();

            // Ensure that user is authorized.
            if (!currentUser.CanCreateTasks ||
                job.Customer.OrganizationId != currentUser.OrganizationId)
                return Forbid();

            // Check for duplicate task numbers in the organization.
            var isNumberDuplicated = _context.Tasks
                .Include(t => t.Job.Customer)
                .Where(t => t.Job.Customer.OrganizationId == currentUser.OrganizationId)
                .Where(t => t.Number == task.Number)
                .Any();

            if (isNumberDuplicated)
                return BadRequest();

            // Determine the order of this task.
            var lastOrder = _context.Tasks
                .Where(t => t.JobId == job.Id)
                .Max(t => (int?)t.Order);

            // Auto-generated
            task.CreatedAt = DateTime.UtcNow;
            task.JobId = job.Id;
            task.Order = lastOrder.HasValue ? lastOrder.Value + 10 : 0;

            // Validate the model.
            ModelState.ClearValidationState(nameof(task));
            if (!TryValidateModel(task, nameof(task)))
            {
                var errors = new List<string>();

                foreach (var modelStateKey in ModelState.Keys)
                {
                    var value = ModelState[modelStateKey];

                    if (value == null) continue;

                    foreach (var error in value.Errors)
                    {
                        errors.Add(error.ErrorMessage);
                    }
                }

                var message = string.Join(", ", errors);
                return BadRequest(message);
            }

            _context.Tasks.Add(task);

            _context.SaveChanges();

            return Created("", task);
        }

        // PATCH: odata/Tasks(5)
        public IActionResult Patch([FromODataUri] int key, Delta<Brizbee.Core.Models.Task> patch)
        {
            var currentUser = CurrentUser();

            var task = _context.Tasks
                .Include(t => t.Job.Customer)
                .Where(t => t.Id == key)
                .FirstOrDefault();

            // Ensure that object was found.
            if (task == null) return NotFound();

            // Ensure that user is authorized.
            if (!currentUser.CanModifyTasks ||
                task.Job.Customer.OrganizationId != currentUser.OrganizationId)
                return Forbid();

            // Do not allow modifying some properties.
            if (patch.GetChangedPropertyNames().Contains("JobId") ||
                patch.GetChangedPropertyNames().Contains("CreatedAt") ||
                patch.GetChangedPropertyNames().Contains("Id"))
            {
                return BadRequest("Cannot modify the JobId, CreatedAt, or Id.");
            }

            // Peform the update.
            patch.Patch(task);

            // Validate the model.
            ModelState.ClearValidationState(nameof(task));
            if (!TryValidateModel(task, nameof(task)))
                return BadRequest();


            _context.SaveChanges();

            return NoContent();
        }

        // DELETE: odata/Tasks(5)
        public IActionResult Delete([FromODataUri] int key)
        {
            var currentUser = CurrentUser();

            var task = _context.Tasks
                .Include(t => t.Job.Customer)
                .Where(t => t.Id == key)
                .FirstOrDefault();

            // Ensure that object was found.
            if (task == null) return NotFound();

            // Ensure that user is authorized.
            if (!currentUser.CanDeleteTasks ||
                task.Job.Customer.OrganizationId != currentUser.OrganizationId)
                return BadRequest();

            // Delete the object itself.
            _context.Tasks.Remove(task);

            _context.SaveChanges();

            return NoContent();
        }

        // POST: odata/Tasks/NextNumber
        [HttpPost]
        public IActionResult NextNumber()
        {
            var organizationId = CurrentUser().OrganizationId;
            var customers = _context.Customers
                .Where(c => c.OrganizationId == organizationId)
                .Select(c => c.Id);
            var jobs = _context.Jobs
                .Where(j => customers.Contains(j.CustomerId))
                .Select(j => j.Id);
            var max = _context.Tasks
                .Where(t => jobs.Contains(t.JobId))
                .Select(t => t.Number)
                .Max();
            if (max == null)
            {
                return Ok("1000");
            }
            else
            {
                var service = new SecurityService();
                var next = service.NxtKeyCode(max);
                return Ok(next);
            }
        }

        // GET: odata/Tasks/Search
        [HttpGet]
        public IActionResult Search([FromODataUri] string Number)
        {
            var currentUser = CurrentUser();

            // Search only tasks that belong to customers in the organization
            var task = _context.Tasks
                .Include("Job")
                .Include("Job.Customer")
                .Where(t => t.Job.Customer.OrganizationId == currentUser.OrganizationId)
                .Where(t => t.Number == Number)
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Number,
                    t.CreatedAt,
                    t.JobId,
                    Job = new
                    {
                        t.Job.Id,
                        t.Job.Name,
                        t.Job.Number,
                        t.Job.CreatedAt,
                        t.Job.CustomerId,
                        Customer = new
                        {
                            t.Job.Customer.Id,
                            t.Job.Customer.Name,
                            t.Job.Customer.Number,
                            t.Job.Customer.CreatedAt
                        }
                    }
                })
                .FirstOrDefault();

            if (task == null)
            {
                return NotFound();
            }

            return new JsonResult(task);
        }

        // GET: odata/Tasks/ForPunches
        [HttpGet]
        [EnableQuery(PageSize = 1000, MaxExpansionDepth = 2)]
        public IQueryable<Brizbee.Core.Models.Task> ForPunches(string InAt, string OutAt)
        {
            var min = DateTime.Parse(InAt);
            var max = DateTime.Parse(OutAt);
            var currentUser = CurrentUser();
            int[] userIds = _context.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id)
                .ToArray();
            var taskIds = _context.Punches
                .Where(p => userIds.Contains(p.UserId))
                .Where(p => p.InAt.Date >= min && p.InAt.Date <= max)
                .GroupBy(p => p.TaskId)
                .Select(g => g.Key);

            return _context.Tasks.Where(t => taskIds.Contains(t.Id));
        }

        private User CurrentUser()
        {
            var type = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
            var sub = HttpContext.User.Claims.FirstOrDefault(c => c.Type == type).Value;
            var currentUserId = int.Parse(sub);
            return _context.Users
                .Where(u => u.Id == currentUserId)
                .FirstOrDefault();
        }
    }
}