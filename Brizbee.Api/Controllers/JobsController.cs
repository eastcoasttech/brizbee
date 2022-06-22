//
//  JobsController.cs
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
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.Json;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace Brizbee.Api.Controllers
{
    public class JobsController : ODataController
    {
        private readonly IConfiguration _configuration;
        private readonly SqlContext _context;

        public JobsController(IConfiguration configuration, SqlContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        // GET: odata/Jobs
        [HttpGet]
        [EnableQuery(PageSize = 1000)]
        public IQueryable<Job> GetJobs()
        {
            var currentUser = CurrentUser();
            var customerIds = _context.Customers
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Select(c => c.Id);

            return _context.Jobs
                .Where(j => customerIds.Contains(j.CustomerId));
        }

        // GET: odata/Jobs/Open
        [HttpGet]
        [EnableQuery(PageSize = 1000, MaxExpansionDepth = 1)]
        public IQueryable<Job> Open()
        {
            var currentUser = CurrentUser();

            return _context.Jobs
                .Include(j => j.Customer)
                .Where(j => j.Customer!.OrganizationId == currentUser.OrganizationId)
                .Where(j => new string[] { "Open", "Needs Invoice" }.Contains(j.Status));
        }

        // GET: odata/Jobs/Closed
        [HttpGet]
        [EnableQuery(PageSize = 1000, MaxExpansionDepth = 1)]
        public IQueryable<Job> Closed()
        {
            var currentUser = CurrentUser();

            return _context.Jobs
                .Include(j => j.Customer)
                .Where(j => j.Customer!.OrganizationId == currentUser.OrganizationId)
                .Where(j => new string[] { "Merged", "Closed" }.Contains(j.Status));
        }

        // GET: odata/Jobs(5)
        [HttpGet]
        [EnableQuery]
        public SingleResult<Job> GetJob([FromODataUri] int key)
        {
            var currentUser = CurrentUser();
            var customerIds = _context.Customers
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Select(c => c.Id);

            return SingleResult.Create(_context.Jobs
                .Where(j => customerIds.Contains(j.CustomerId))
                .Where(j => j.Id == key));
        }

        // POST: odata/Jobs
        public IActionResult Post([FromBody] Job job)
        {
            var currentUser = CurrentUser();

            // Ensure user is an administrator.
            if (!currentUser.CanCreateProjects)
                return Forbid();

            var customer = _context.Customers
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Where(c => c.Id == job.CustomerId)
                .FirstOrDefault();

            if (customer == null)
                return BadRequest();

            // Auto-generated.
            job.CreatedAt = DateTime.UtcNow;
            job.CustomerId = customer.Id;

            // Prepare to create tasks automatically.
            var taskTemplateId = job.TaskTemplateId;

            // Validate the model.
            ModelState.ClearValidationState(nameof(job));
            if (!TryValidateModel(job, nameof(job)))
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

            _context.Jobs.Add(job);

            _context.SaveChanges();

            if (taskTemplateId.HasValue)
            {
                var taskTemplate = _context.TaskTemplates
                    .Where(c => c.OrganizationId == currentUser.OrganizationId)
                    .Where(c => c.Id == taskTemplateId.Value)
                    .FirstOrDefault();

                if (taskTemplate != null)
                {
                    var service = new SecurityService();

                    var items = JsonSerializer.Deserialize<TemplateItem[]>(taskTemplate.Template);

                    // Start ordering.
                    var order = 10;

                    foreach (var item in items)
                    {
                        // Get the next task number.
                        var maxSql = @"
                            SELECT
	                            MAX(CAST([T].[Number] AS INT))
                            FROM
                                [Tasks] AS [T]
                            JOIN
	                            [Jobs] AS [J] ON [J].[Id] = [T].[JobId]
                            JOIN
	                            [Customers] AS [C] ON [C].[Id] = [J].[CustomerId]
                            WHERE
	                            [C].[OrganizationId] = @OrganizationId;";
                        var max = _context.Database.GetDbConnection().QuerySingle<int?>(maxSql, new { OrganizationId = currentUser.OrganizationId });

                        // Either start at the default or increment.
                        var number = !max.HasValue ? "1000" : (max.Value + 1).ToString(); //service.NxtKeyCode(max);

                        // Find the base payroll rate for the new task.
                        var basePayrollRate = _context.Rates
                            .Where(r => r.OrganizationId == currentUser.OrganizationId)
                            .Where(r => r.Type == "Payroll")
                            .Where(r => r.Name == item.BasePayrollRate)
                            .Where(r => r.IsDeleted == false)
                            .FirstOrDefault();

                        // Find the base service rate for the new task.
                        var baseServiceRate = _context.Rates
                            .Where(r => r.OrganizationId == currentUser.OrganizationId)
                            .Where(r => r.Type == "Service")
                            .Where(r => r.Name == item.BaseServiceRate)
                            .Where(r => r.IsDeleted == false)
                            .FirstOrDefault();

                        // Populate the new task.
                        var task = new Brizbee.Core.Models.Task()
                        {
                            Name = item.Name,
                            CreatedAt = DateTime.UtcNow,
                            JobId = job.Id,
                            Group = item.Group,
                            Order = order,
                            Number = number,
                            BasePayrollRateId = basePayrollRate == null ? (int?)null : basePayrollRate.Id,
                            BaseServiceRateId = baseServiceRate == null ? (int?)null : baseServiceRate.Id
                        };

                        _context.Tasks.Add(task);
                        _context.SaveChanges();

                        // Increment the order.
                        order = order + 10;
                    }
                }
            }

            // Attempt to send notifications.
            try
            {
                var mobileNumbers = _context.Users
                    .Where(u => u.IsDeleted == false)
                    .Where(u => u.OrganizationId == currentUser.OrganizationId)
                    .Where(u => !string.IsNullOrEmpty(u.NotificationMobileNumbers))
                    .Select(u => u.NotificationMobileNumbers)
                    .ToArray();

                var accountSid = _configuration["TwilioNotificationsAccountSid"];
                var authToken = _configuration["TwilioNotificationsAccountToken"];
                var fromMobileNumber = _configuration["TwilioNotificationsMobileNumber"];

                TwilioClient.Init(accountSid, authToken);

                foreach (var mobileNumber in mobileNumbers)
                {
                    var message = MessageResource.Create(
                        body: $"Project Added {job.Number} - {job.Name} for Customer {job.Customer.Name}",
                        from: new Twilio.Types.PhoneNumber(fromMobileNumber),
                        to: new Twilio.Types.PhoneNumber(mobileNumber)
                    );
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning(ex.ToString());
            }

            return Created("", job);
        }

        // PATCH: odata/Jobs(5)
        public IActionResult Patch([FromODataUri] int key, Delta<Job> patch)
        {
            var currentUser = CurrentUser();

            var job = _context.Jobs
                .Include(j => j.Customer)
                .Where(j => j.Customer.OrganizationId == currentUser.OrganizationId)
                .Where(j => j.Id == key)
                .FirstOrDefault();

            // Ensure that object was found.
            if (job == null) return NotFound();

            // Ensure that user is authorized.
            if (!currentUser.CanModifyProjects ||
                currentUser.OrganizationId != job.Customer.OrganizationId)
                return Forbid();

            // Do not allow modifying some properties.
            if (patch.GetChangedPropertyNames().Contains("CustomerId") ||
                patch.GetChangedPropertyNames().Contains("Id") ||
                patch.GetChangedPropertyNames().Contains("CreatedAt"))
            {
                return BadRequest("Not authorized to modify the CustomerId, CreatedAt, or Id.");
            }

            // Peform the update
            patch.Patch(job);

            // Validate the model.
            ModelState.ClearValidationState(nameof(job));
            if (!TryValidateModel(job, nameof(job)))
                return BadRequest();

            _context.SaveChanges();

            return NoContent();
        }

        // DELETE: odata/Jobs(5)
        public IActionResult Delete([FromODataUri] int key)
        {
            var currentUser = CurrentUser();

            var job = _context.Jobs
                .Include(j => j.Customer)
                .Where(j => j.Customer.OrganizationId == currentUser.OrganizationId)
                .Where(j => j.Id == key)
                .FirstOrDefault();

            // Ensure that object was found.
            if (job == null) return NotFound();

            // Ensure that user is authorized.
            if (!currentUser.CanDeleteProjects ||
                currentUser.OrganizationId != job.Customer.OrganizationId)
                return Forbid();

            // Remove tasks for the job.
            var tasks = _context.Tasks.Where(t => t.JobId == key).ToList();
            _context.Tasks.RemoveRange(tasks);

            // Delete the job itself.
            _context.Jobs.Remove(job);

            _context.SaveChanges();

            return NoContent();
        }

        // POST: odata/Jobs/NextNumber
        [HttpPost]
        public IActionResult NextNumber()
        {
            var currentUser = CurrentUser();

            var max = _context.Jobs
                .Include(j => j.Customer)
                .Where(j => j.Customer.OrganizationId == currentUser.OrganizationId)
                .Select(j => j.Number)
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


    public class TemplateItem
    {
        public string? Name { get; set; }

        public string? Group { get; set; }

        public string? BasePayrollRate { get; set; }

        public string? BaseServiceRate { get; set; }
    }
}