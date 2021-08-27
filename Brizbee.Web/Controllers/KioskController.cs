//
//  KioskController.cs
//  BRIZBEE API
//
//  Copyright (C) 2021 East Coast Technology Services, LLC
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
using Brizbee.Web.Serialization.Expanded;
using Dapper;
using System;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;

namespace Brizbee.Web.Controllers
{
    public class KioskController : ApiController
    {
        private SqlContext _context = new SqlContext();
        private PunchRepository repo = new PunchRepository();

        // POST: api/Kiosk/PunchIn
        [HttpPost]
        [Route("api/Kiosk/PunchIn")]
        [ResponseType(typeof(Punch))]
        public IHttpActionResult PunchIn([FromUri] int taskId, [FromUri] string timeZone,
            [FromUri] string latitude, [FromUri] string longitude, [FromUri] string sourceHardware,
            [FromUri] string sourceOperatingSystem, [FromUri] string sourceOperatingSystemVersion,
            [FromUri] string sourceBrowser, [FromUri] string sourceBrowserVersion)
        {
            string sourceHostname = "";
            string sourceIpAddress = "";

            try
            {
                // Attempt to get the client hostname.
                sourceHostname = HttpContext.Current.Request.UserHostName;
            }
            catch { }

            try
            {
                // Attempt to get the client IP address.
                sourceIpAddress = HttpContext.Current.Request.UserHostAddress;
            }
            catch { }

            var currentUser = CurrentUser();

            var task = _context.Tasks
                .Include(t => t.Job.Customer)
                .Where(t => t.Job.Customer.OrganizationId == currentUser.OrganizationId)
                .Where(t => t.Id == taskId)
                .FirstOrDefault();

            // Ensure that task was found.
            if (task == null) return NotFound();

            // Ensure job is open.
            if (task.Job.Status != "Open")
                return BadRequest();

            try
            {
                var punch = repo.PunchIn(
                    taskId,
                    CurrentUser(),
                    "",
                    timeZone,
                    latitude,
                    longitude,
                    sourceHardware,
                    sourceHostname,
                    sourceIpAddress,
                    sourceOperatingSystem,
                    sourceOperatingSystemVersion,
                    sourceBrowser,
                    sourceBrowserVersion);

                return Ok(punch);
            }
            catch (DbEntityValidationException ex)
            {
                string message = "";

                foreach (var eve in ex.EntityValidationErrors)
                {
                    foreach (var ve in eve.ValidationErrors)
                    {
                        message += string.Format("{0} has error '{1}'; ", ve.PropertyName, ve.ErrorMessage);
                    }
                }

                Trace.TraceError(ex.ToString());
                Trace.TraceError(ex.Message);
                return BadRequest(message);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                return BadRequest(ex.Message);
            }
        }

        // POST: api/Kiosk/PunchOut
        [HttpPost]
        [Route("api/Kiosk/PunchOut")]
        [ResponseType(typeof(Punch))]
        public IHttpActionResult PunchOut([FromUri] string timeZone,
            [FromUri] string latitude, [FromUri] string longitude, [FromUri] string sourceHardware,
            [FromUri] string sourceOperatingSystem, [FromUri] string sourceOperatingSystemVersion,
            [FromUri] string sourceBrowser, [FromUri] string sourceBrowserVersion)
        {
            string sourceHostname = "";
            string sourceIpAddress = "";

            try
            {
                // Attempt to get the client hostname.
                sourceHostname = HttpContext.Current.Request.UserHostName;
            }
            catch { }

            try
            {
                // Attempt to get the client IP address.
                sourceIpAddress = HttpContext.Current.Request.UserHostAddress;
            }
            catch { }

            try
            {
                var punch = repo.PunchOut(
                    CurrentUser(),
                    "",
                    timeZone,
                    latitude,
                    longitude,
                    sourceHardware,
                    sourceHostname,
                    sourceIpAddress,
                    sourceOperatingSystem,
                    sourceOperatingSystemVersion,
                    sourceBrowser,
                    sourceBrowserVersion);

                return Ok(punch);
            }
            catch (DbEntityValidationException ex)
            {
                string message = "";

                foreach (var eve in ex.EntityValidationErrors)
                {
                    foreach (var ve in eve.ValidationErrors)
                    {
                        message += string.Format("{0} has error '{1}'; ", ve.PropertyName, ve.ErrorMessage);
                    }
                }

                Trace.TraceError(ex.ToString());
                Trace.TraceError(ex.Message);
                return BadRequest(message);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                return BadRequest(ex.Message);
            }
        }

        // GET: api/Kiosk/CurrentPunch
        [HttpGet]
        [Route("api/Kiosk/CurrentPunch")]
        [ResponseType(typeof(Punch))]
        public IHttpActionResult CurrentPunch()
        {
            User currentUser = CurrentUser();

            Punch currentPunch = null;
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlContext"].ToString()))
            {
                connection.Open();

                var currentPunchSql = @"
                    SELECT TOP(1)
                        [P].[Id] AS Punch_Id,
                        [P].[CommitId] AS Punch_CommitId,
                        [P].[CreatedAt] AS Punch_CreatedAt,
                        [P].[Guid] AS Punch_Guid,
                        [P].[InAt] AS Punch_InAt,
                        [P].[InAtTimeZone] AS Punch_InAtTimeZone,
                        [P].[LatitudeForInAt] AS Punch_LatitudeForInAt,
                        [P].[LongitudeForInAt] AS Punch_LongitudeForInAt,
                        [P].[LatitudeForOutAt] AS Punch_LatitudeForOutAt,
                        [P].[LongitudeForOutAt] AS Punch_LongitudeForOutAt,
                        [P].[OutAt] AS Punch_OutAt,
                        [P].[OutAtTimeZone] AS Punch_OutAtTimeZone,
                        [P].[SourceForInAt] AS Punch_SourceForInAt,
                        [P].[SourceForOutAt] AS Punch_SourceForOutAt,
                        [P].[TaskId] AS Punch_TaskId,
                        [P].[UserId] AS Punch_UserId,
                        [P].[InAtSourceHardware] AS Punch_InAtSourceHardware,
                        [P].[InAtSourceHostname] AS Punch_InAtSourceHostname,
                        [P].[InAtSourceIpAddress] AS Punch_InAtSourceIpAddress,
                        [P].[InAtSourceOperatingSystem] AS Punch_InAtSourceOperatingSystem,
                        [P].[InAtSourceOperatingSystemVersion] AS Punch_InAtSourceOperatingSystemVersion,
                        [P].[InAtSourceBrowser] AS Punch_InAtSourceBrowser,
                        [P].[InAtSourceBrowserVersion] AS Punch_InAtSourceBrowserVersion,
                        [P].[InAtSourcePhoneNumber] AS Punch_InAtSourcePhoneNumber,
                        [P].[OutAtSourceHardware] AS Punch_OutAtSourceHardware,
                        [P].[OutAtSourceHostname] AS Punch_OutAtSourceHostname,
                        [P].[OutAtSourceIpAddress] AS Punch_OutAtSourceIpAddress,
                        [P].[OutAtSourceOperatingSystem] AS Punch_OutAtSourceOperatingSystem,
                        [P].[OutAtSourceOperatingSystemVersion] AS Punch_OutAtSourceOperatingSystemVersion,
                        [P].[OutAtSourceBrowser] AS Punch_OutAtSourceBrowser,
                        [P].[OutAtSourceBrowserVersion] AS Punch_OutAtSourceBrowserVersion,
                        [P].[OutAtSourcePhoneNumber] AS Punch_OutAtSourcePhoneNumber,

                        [C].[Id] AS Customer_Id,
                        [C].[CreatedAt] AS Customer_CreatedAt,
                        [C].[Description] AS Customer_Description,
                        [C].[Name] AS Customer_Name,
                        [C].[Number] AS Customer_Number,
                        [C].[OrganizationId] AS Customer_OrganizationId,

                        [J].[Id] AS Job_Id,
                        [J].[CreatedAt] AS Job_CreatedAt,
                        [J].[CustomerId] AS Job_CustomerId,
                        [J].[Description] AS Job_Description,
                        [J].[Name] AS Job_Name,
                        [J].[Number] AS Job_Number,

                        [T].[Id] AS Task_Id,
                        [T].[CreatedAt] AS Task_CreatedAt,
                        [T].[JobId] AS Task_JobId,
                        [T].[Name] AS Task_Name,
                        [T].[Number] AS Task_Number
                    FROM
                        [Punches] AS P
                    INNER JOIN
                        [Tasks] AS T ON [P].[TaskId] = [T].[Id]
                    INNER JOIN
                        [Jobs] AS J ON [T].[JobId] = [J].[Id]
                    INNER JOIN
                        [Customers] AS C ON [J].[CustomerId] = [C].[Id]
                    WHERE
                        [P].[UserId] = @UserId AND
                        [P].[OutAt] IS NULL
                    ORDER BY
                        [InAt] DESC;";

                var results = connection.Query<PunchExpanded>(currentPunchSql, new { UserId = currentUser.Id });

                if (results.Any())
                {
                    var result = results.FirstOrDefault();
                    currentPunch = new Punch()
                    {
                        Id = result.Punch_Id,
                        CommitId = result.Punch_CommitId,
                        CreatedAt = result.Punch_CreatedAt,
                        Guid = result.Punch_Guid,
                        InAt = result.Punch_InAt.ToUniversalTime(),
                        InAtTimeZone = result.Punch_InAtTimeZone,
                        LatitudeForInAt = result.Punch_LatitudeForInAt,
                        LongitudeForInAt = result.Punch_LongitudeForInAt,
                        LatitudeForOutAt = result.Punch_LatitudeForOutAt,
                        LongitudeForOutAt = result.Punch_LongitudeForOutAt,
                        OutAt = result.Punch_OutAt.HasValue ? result.Punch_OutAt.Value.ToUniversalTime() : default(DateTime?),
                        OutAtTimeZone = result.Punch_OutAtTimeZone,
                        SourceForInAt = result.Punch_SourceForInAt,
                        SourceForOutAt = result.Punch_SourceForOutAt,
                        TaskId = result.Punch_TaskId,
                        UserId = result.Punch_UserId,
                        InAtSourceHardware = result.Punch_InAtSourceHardware,
                        InAtSourceHostname = result.Punch_InAtSourceHostname,
                        InAtSourceIpAddress = result.Punch_InAtSourceIpAddress,
                        InAtSourceOperatingSystem = result.Punch_InAtSourceOperatingSystem,
                        InAtSourceOperatingSystemVersion = result.Punch_InAtSourceOperatingSystemVersion,
                        InAtSourceBrowser = result.Punch_InAtSourceBrowser,
                        InAtSourceBrowserVersion = result.Punch_InAtSourceBrowserVersion,
                        InAtSourcePhoneNumber = result.Punch_InAtSourcePhoneNumber,
                        OutAtSourceHardware = result.Punch_OutAtSourceHardware,
                        OutAtSourceHostname = result.Punch_OutAtSourceHostname,
                        OutAtSourceIpAddress = result.Punch_OutAtSourceIpAddress,
                        OutAtSourceOperatingSystem = result.Punch_OutAtSourceOperatingSystem,
                        OutAtSourceOperatingSystemVersion = result.Punch_OutAtSourceOperatingSystemVersion,
                        OutAtSourceBrowser = result.Punch_OutAtSourceBrowser,
                        OutAtSourceBrowserVersion = result.Punch_OutAtSourceBrowserVersion,
                        OutAtSourcePhoneNumber = result.Punch_OutAtSourcePhoneNumber,
                        Task = new Task()
                        {
                            Id = result.Task_Id,
                            CreatedAt = result.Task_CreatedAt,
                            JobId = result.Task_JobId,
                            Name = result.Task_Name,
                            Number = result.Task_Number,
                            Job = new Job()
                            {
                                Id = result.Job_Id,
                                CreatedAt = result.Job_CreatedAt,
                                CustomerId = result.Job_CustomerId,
                                Description = result.Job_Description,
                                Name = result.Job_Name,
                                Number = result.Job_Number,
                                Customer = new Customer()
                                {
                                    Id = result.Customer_Id,
                                    CreatedAt = result.Customer_CreatedAt,
                                    Description = result.Customer_Description,
                                    Name = result.Customer_Name,
                                    Number = result.Customer_Number,
                                    OrganizationId = result.Customer_OrganizationId
                                }
                            }
                        }
                    };
                }

                connection.Close();
            }

            return Ok(currentPunch);
        }

        // POST: api/Kiosk/Timecard
        [HttpPost]
        [Route("api/Kiosk/Timecard")]
        [ResponseType(typeof(TimesheetEntry))]
        public IHttpActionResult Timecard([FromUri] int taskId, [FromUri] DateTime enteredAt, [FromUri] int minutes, [FromUri] string notes)
        {
            var currentUser = CurrentUser();

            var task = _context.Tasks
                .Include(t => t.Job.Customer)
                .Where(t => t.Job.Customer.OrganizationId == currentUser.OrganizationId)
                .Where(t => t.Id == taskId)
                .FirstOrDefault();

            // Ensure that task was found.
            if (task == null) return NotFound();

            // Ensure job is open.
            if (task.Job.Status != "Open")
                return BadRequest();

            try
            {
                var timesheetEntry = _context.TimesheetEntries.Add(new TimesheetEntry()
                {
                    CreatedAt = DateTime.UtcNow,
                    EnteredAt = enteredAt,
                    Minutes = minutes,
                    Notes = notes,
                    TaskId = taskId,
                    UserId = currentUser.Id
                });
                _context.SaveChanges();

                return Ok(timesheetEntry);
            }
            catch (DbEntityValidationException ex)
            {
                string message = "";

                foreach (var eve in ex.EntityValidationErrors)
                {
                    foreach (var ve in eve.ValidationErrors)
                    {
                        message += string.Format("{0} has error '{1}'; ", ve.PropertyName, ve.ErrorMessage);
                    }
                }

                Trace.TraceError(ex.ToString());
                Trace.TraceError(ex.Message);
                return BadRequest(message);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                return BadRequest(ex.Message);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }

        public User CurrentUser()
        {
            if (ActionContext.RequestContext.Principal.Identity.Name.Length > 0)
            {
                var currentUserId = int.Parse(ActionContext.RequestContext.Principal.Identity.Name);
                return _context.Users
                    .Where(u => u.Id == currentUserId)
                    .FirstOrDefault();
            }
            else
            {
                return null;
            }
        }
    }
}