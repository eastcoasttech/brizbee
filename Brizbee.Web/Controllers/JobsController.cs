//
//  JobsController.cs
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
using Dapper;
using Microsoft.AspNet.OData;
using System;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.SqlServer;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Web.Http;
using Twilio;
using Twilio.Clients;
using Twilio.Rest.Api.V2010.Account;

namespace Brizbee.Web.Controllers
{
    public class JobsController : BaseODataController
    {
        private SqlContext db = new SqlContext();

        // GET: odata/Jobs
        [EnableQuery(PageSize = 1000)]
        public IQueryable<Job> GetJobs()
        {
            var currentUser = CurrentUser();
            var customerIds = db.Customers
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Select(c => c.Id);

            return db.Jobs
                .Where(j => customerIds.Contains(j.CustomerId));
        }

        // GET: odata/Jobs/Default.Open
        [HttpGet]
        [EnableQuery(PageSize = 1000, MaxExpansionDepth = 1)]
        public IQueryable<Job> Open()
        {
            var currentUser = CurrentUser();
            var customerIds = db.Customers
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Select(c => c.Id);

            return db.Jobs
                .Where(j => customerIds.Contains(j.CustomerId))
                .Where(j => j.Status != "Closed")
                .Where(j => j.Status != "Merged");
        }

        // GET: odata/Jobs(5)
        [EnableQuery]
        public SingleResult<Job> GetJob([FromODataUri] int key)
        {
            var currentUser = CurrentUser();
            var customerIds = db.Customers
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Select(c => c.Id);

            return SingleResult.Create(db.Jobs
                .Where(j => customerIds.Contains(j.CustomerId))
                .Where(j => j.Id == key));
        }

        // POST: odata/Jobs
        public IHttpActionResult Post(Job job)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            var currentUser = CurrentUser();

            // Ensure user is an administrator.
            if (currentUser.Role != "Administrator")
                return BadRequest();

            var customer = db.Customers
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Where(c => c.Id == job.CustomerId)
                .FirstOrDefault();

            if (customer == null)
                return BadRequest();

            // Auto-generated
            job.CreatedAt = DateTime.UtcNow;
            job.CustomerId = customer.Id;

            // Prepare to create tasks automatically.
            var taskTemplateId = job.TaskTemplateId;

            db.Jobs.Add(job);

            db.SaveChanges();

            if (taskTemplateId.HasValue)
            {
                var taskTemplate = db.TaskTemplates
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
                        var max = db.Database.Connection.QuerySingle<int?>(maxSql, new { OrganizationId = currentUser.OrganizationId });

                        // Either start at the default or increment.
                        var number = !max.HasValue ? "1000" : (max.Value + 1).ToString(); //service.NxtKeyCode(max);

                        // Find the base payroll rate for the new task.
                        var basePayrollRate = db.Rates
                            .Where(r => r.OrganizationId == currentUser.OrganizationId)
                            .Where(r => r.Type == "Payroll")
                            .Where(r => r.Name == item.BasePayrollRate)
                            .Where(r => r.IsDeleted == false)
                            .FirstOrDefault();

                        // Find the base service rate for the new task.
                        var baseServiceRate = db.Rates
                            .Where(r => r.OrganizationId == currentUser.OrganizationId)
                            .Where(r => r.Type == "Service")
                            .Where(r => r.Name == item.BaseServiceRate)
                            .Where(r => r.IsDeleted == false)
                            .FirstOrDefault();

                        // Populate the new task.
                        var task = new Task()
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

                        db.Tasks.Add(task);
                        db.SaveChanges();

                        // Increment the order.
                        order = order + 10;
                    }
                }
            }

            // Attempt to send notifications.
            try
            {
                var mobileNumbers = db.Users
                    .Where(u => u.IsDeleted == false)
                    .Where(u => u.OrganizationId == currentUser.OrganizationId)
                    .Where(u => !string.IsNullOrEmpty(u.NotificationMobileNumbers))
                    .Select(u => u.NotificationMobileNumbers)
                    .ToArray();

                var accountSid = ConfigurationManager.AppSettings["TwilioNotificationsAccountSid"].ToString();
                var authToken = ConfigurationManager.AppSettings["TwilioNotificationsAccountToken"].ToString();
                var fromMobileNumber = ConfigurationManager.AppSettings["TwilioNotificationsMobileNumber"].ToString();

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

            return Created(job);
        }

        // PATCH: odata/Jobs(5)
        [AcceptVerbs("PATCH", "MERGE")]
        public IHttpActionResult Patch([FromODataUri] int key, Delta<Job> patch)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUser = CurrentUser();

            var job = db.Jobs
                .Include("Customer")
                .Where(j => j.Id == key)
                .FirstOrDefault();

            // Ensure that object was found.
            if (job == null) return NotFound();

            // Ensure that user is authorized.
            if (currentUser.Role != "Administrator" ||
                currentUser.OrganizationId != job.Customer.OrganizationId)
                throw new Exception("Not authorized to modify the object");

            // Do not allow modifying some properties.
            if (patch.GetChangedPropertyNames().Contains("CustomerId"))
            {
                return BadRequest("Not authorized to modify the CustomerId");
            }

            // Peform the update
            patch.Patch(job);

            db.SaveChanges();

            return Updated(job);
        }

        // DELETE: odata/Jobs(5)
        public IHttpActionResult Delete([FromODataUri] int key)
        {
            var currentUser = CurrentUser();

            // Only permit administrators
            if (currentUser.Role != "Administrator")
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }

            // Only look within the jobs that belong to the organization.
            var customerIds = db.Customers
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Select(c => c.Id);
            var job = db.Jobs
                .Where(j => customerIds.Contains(j.CustomerId))
                .Where(j => j.Id == key)
                .FirstOrDefault();

            // Remove tasks for the job.
            var tasks = db.Tasks.Where(t => t.JobId == key).ToList();
            db.Tasks.RemoveRange(tasks);

            // Delete the job itself.
            db.Jobs.Remove(job);

            db.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: odata/Jobs/Default.NextNumber
        public IHttpActionResult NextNumber()
        {
            var currentUser = CurrentUser();

            var max = db.Jobs
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }


    public class TemplateItem
    {
        public string Name { get; set; }

        public string Group { get; set; }

        public string BasePayrollRate { get; set; }

        public string BaseServiceRate { get; set; }
    }
}