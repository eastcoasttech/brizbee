//
//  TasksController.cs
//  BRIZBEE API
//
//  Copyright (C) 2020 East Coast Technology Services, LLC
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

using Brizbee.Common.Models;
using Brizbee.Web.Repositories;
using Microsoft.AspNet.OData;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Http;

namespace Brizbee.Web.Controllers
{
    public class TasksController : BaseODataController
    {
        private SqlContext db = new SqlContext();

        // GET: odata/Tasks
        [EnableQuery(PageSize = 1000, MaxExpansionDepth = 3)]
        public IQueryable<Task> GetTasks()
        {
            var currentUser = CurrentUser();

            var customerIds = db.Customers
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Select(c => c.Id);
            var jobIds = db.Jobs
                .Where(j => customerIds.Contains(j.CustomerId))
                .Select(j => j.Id);
            return db.Tasks
                .Where(t => jobIds.Contains(t.JobId));
        }

        // GET: odata/Tasks(5)
        [EnableQuery(MaxExpansionDepth = 3)]
        public SingleResult<Task> GetTask ([FromODataUri] int key)
        {
            var currentUser = CurrentUser();

            var customerIds = db.Customers
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Select(c => c.Id);
            var jobIds = db.Jobs
                .Where(j => customerIds.Contains(j.CustomerId))
                .Select(j => j.Id);

            return SingleResult.Create(db.Tasks
                .Where(t => jobIds.Contains(t.JobId))
                .Where(t => t.Id == key));
        }

        // POST: odata/Tasks
        public IHttpActionResult Post(Task task)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUser = CurrentUser();

            var job = db.Jobs
                .Include("Customer")
                .Where(j => j.Id == task.JobId)
                .FirstOrDefault();

            // Only permit administrators of the same organization.
            if (currentUser.Role != "Administrator" ||
                job.Customer.OrganizationId != currentUser.OrganizationId)
                return BadRequest();

            var max = db.Tasks
                .Where(t => t.JobId == job.Id)
                .Max(t => (int?)t.Order);

            // Auto-generated
            task.CreatedAt = DateTime.UtcNow;
            task.JobId = job.Id;
            task.Order = max.HasValue ? max.Value + 10 : 0;

            db.Tasks.Add(task);

            db.SaveChanges();

            return Created(task);
        }

        // PATCH: odata/Tasks(5)
        [AcceptVerbs("PATCH", "MERGE")]
        public IHttpActionResult Patch([FromODataUri] int key, Delta<Task> patch)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUser = CurrentUser();

            var task = db.Tasks
                .Include("Job")
                .Include("Job.Customer")
                .Where(t => t.Id == key)
                .FirstOrDefault();

            // Only permit administrators of the same organization.
            if (currentUser.Role != "Administrator" ||
                task.Job.Customer.OrganizationId != currentUser.OrganizationId)
                return BadRequest();

            // Ensure that object was found
            if (task == null)
                return NotFound();

            // Do not allow modifying some properties
            if (patch.GetChangedPropertyNames().Contains("JobId"))
            {
                return BadRequest("Cannot modify the JobId");
            }

            // Peform the update
            patch.Patch(task);

            db.SaveChanges();

            return Updated(task);
        }

        // DELETE: odata/Tasks(5)
        public IHttpActionResult Delete([FromODataUri] int key)
        {
            var currentUser = CurrentUser();

            var task = db.Tasks
                .Include("Job")
                .Include("Job.Customer")
                .Where(t => t.Id == key)
                .FirstOrDefault();

            // Only permit administrators of the same organization.
            if (currentUser.Role != "Administrator" ||
                task.Job.Customer.OrganizationId != currentUser.OrganizationId)
                return BadRequest();

            // Delete the object itself
            db.Tasks.Remove(task);

            db.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: odata/Tasks/Default.NextNumber
        public IHttpActionResult NextNumber()
        {
            var organizationId = CurrentUser().OrganizationId;
            var customers = db.Customers
                .Where(c => c.OrganizationId == organizationId)
                .Select(c => c.Id);
            var jobs = db.Jobs
                .Where(j => customers.Contains(j.CustomerId))
                .Select(j => j.Id);
            var max = db.Tasks
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

        // GET: odata/Tasks/Default.Search
        [HttpGet]
        public IHttpActionResult Search([FromODataUri] string Number)
        {
            var currentUser = CurrentUser();

            // Search only tasks that belong to customers in the organization
            var task = db.Tasks
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

            return Json(task);
        }

        // GET: odata/Tasks/Default.ForPunches
        [HttpGet]
        [EnableQuery(PageSize = 30, MaxExpansionDepth = 2)]
        public IQueryable<Task> ForPunches(string InAt, string OutAt)
        {
            var inAt = DateTime.Parse(InAt);
            var outAt = DateTime.Parse(OutAt);
            var currentUser = CurrentUser();
            int[] userIds = db.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id)
                .ToArray();
            var taskIds = db.Punches
                .Where(p => userIds.Contains(p.UserId))
                .Where(p => DbFunctions.TruncateTime(p.InAt) >= inAt && DbFunctions.TruncateTime(p.OutAt) <= outAt)
                .GroupBy(p => p.TaskId)
                .Select(g => g.Key);

            return db.Tasks.Where(t => taskIds.Contains(t.Id));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}