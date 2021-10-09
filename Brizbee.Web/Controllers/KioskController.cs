//
//  KioskController.cs
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

using Brizbee.Common.Models;
using Brizbee.Common.Serialization;
using Brizbee.Web.Repositories;
using Brizbee.Web.Serialization.Expanded;
using Dapper;
using Newtonsoft.Json;
using NodaTime;
using NodaTime.TimeZones;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Caching;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;

namespace Brizbee.Web.Controllers
{
    public class KioskController : ApiController
    {
        private SqlContext _context = new SqlContext();
        private PunchRepository repo = new PunchRepository();
        private ObjectCache cache = MemoryCache.Default;

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
                return BadRequest("Cannot punch in on tasks for projects that are not open.");

            // Prevent double submission.
            var submission = cache.Get($"submission.punchin.{currentUser.Id}") as bool?;
            if (submission.HasValue)
                return BadRequest("Cannot punch in twice within 5 seconds.");

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

                // Record the submission.
                var policy = new CacheItemPolicy();
                policy.AbsoluteExpiration = DateTime.UtcNow.AddSeconds(5);
                cache.Set($"submission.punchin.{currentUser.Id}", true, policy);

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

            var currentUser = CurrentUser();

            // Prevent double submission.
            var submission = cache.Get($"submission.punchout.{currentUser.Id}") as bool?;
            if (submission.HasValue)
                return BadRequest("Cannot punch out twice within 5 seconds.");

            try
            {
                var punch = repo.PunchOut(
                    currentUser,
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

                // Record the submission.
                var policy = new CacheItemPolicy();
                policy.AbsoluteExpiration = DateTime.UtcNow.AddSeconds(5);
                cache.Set($"submission.punchout.{currentUser.Id}", true, policy);

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

        // GET: api/Kiosk/Punches/Current
        [HttpGet]
        [Route("api/Kiosk/Punches/Current")]
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

            if (currentPunch != null)
                return Ok(currentPunch);
            else
                return Ok(new { });
        }

        // POST: api/Kiosk/TimeCard
        [HttpPost]
        [Route("api/Kiosk/TimeCard")]
        [ResponseType(typeof(TimesheetEntry))]
        public IHttpActionResult TimeCard([FromUri] int taskId, [FromUri] DateTime enteredAt, [FromUri] int minutes, [FromUri] string notes = "")
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
                return BadRequest("Cannot add time cards for projects that are not open.");

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

        // GET: api/Kiosk/SearchTasks
        [HttpGet]
        [Route("api/Kiosk/SearchTasks")]
        [ResponseType(typeof(Task))]
        public IHttpActionResult SearchTasks([FromUri] string taskNumber)
        {
            var currentUser = CurrentUser();

            // Search only tasks that belong to customers in the organization.
            var task = _context.Tasks
                .Include("Job")
                .Include("Job.Customer")
                .Where(t => t.Job.Customer.OrganizationId == currentUser.OrganizationId)
                .Where(t => t.Number == taskNumber)
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

            return Ok(task);
        }

        // GET: api/Kiosk/Customers
        [HttpGet]
        [Route("api/Kiosk/Customers")]
        [ResponseType(typeof(IEnumerable<Customer>))]
        public IHttpActionResult Customers()
        {
            var currentUser = CurrentUser();

            var customers = _context.Customers
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .OrderBy(c => c.Number)
                .Select(c => new
                {
                    c.CreatedAt,
                    c.Description,
                    c.Id,
                    c.Name,
                    c.Number,
                    c.OrganizationId
                })
                .ToList();

            return Ok(customers);
        }

        // GET: api/Kiosk/Projects
        [HttpGet]
        [Route("api/Kiosk/Projects")]
        [ResponseType(typeof(IEnumerable<Job>))]
        public IHttpActionResult Projects([FromUri] int customerId)
        {
            var currentUser = CurrentUser();

            var projects = _context.Jobs
                .Include(j => j.Customer)
                .Where(j => j.Customer.OrganizationId == currentUser.OrganizationId)
                .Where(j => j.CustomerId == customerId)
                .OrderBy(j => j.Number)
                .Select(j => new
                {
                    j.CreatedAt,
                    j.CustomerId,
                    j.CustomerPurchaseOrder,
                    j.CustomerWorkOrder,
                    j.Description,
                    j.Id,
                    j.InvoiceNumber,
                    j.Name,
                    j.Number,
                    j.QuoteNumber,
                    j.Status,
                    j.Taxability,
                    Customer = new
                    {
                        j.Customer.CreatedAt,
                        j.Customer.Description,
                        j.Customer.Id,
                        j.Customer.Name,
                        j.Customer.Number,
                        j.Customer.OrganizationId
                    }
                })
                .ToList();

            return Ok(projects);
        }

        // GET: api/Kiosk/Tasks
        [HttpGet]
        [Route("api/Kiosk/Tasks")]
        [ResponseType(typeof(IEnumerable<Task>))]
        public IHttpActionResult Tasks([FromUri] int projectId)
        {
            var currentUser = CurrentUser();

            var projects = _context.Tasks
                .Include(t => t.Job.Customer)
                .Where(t => t.Job.Customer.OrganizationId == currentUser.OrganizationId)
                .Where(t => t.JobId == projectId)
                .OrderBy(t => t.Number)
                .Select(t => new
                {
                    t.CreatedAt,
                    t.Group,
                    t.Id,
                    t.JobId,
                    t.Name,
                    t.Number,
                    t.Order,
                    Job = new
                    {
                        t.Job.CreatedAt,
                        t.Job.CustomerId,
                        t.Job.CustomerPurchaseOrder,
                        t.Job.CustomerWorkOrder,
                        t.Job.Description,
                        t.Job.Id,
                        t.Job.InvoiceNumber,
                        t.Job.Name,
                        t.Job.Number,
                        t.Job.QuoteNumber,
                        t.Job.Status,
                        t.Job.Taxability,
                        Customer = new
                        {
                            t.Job.Customer.CreatedAt,
                            t.Job.Customer.Description,
                            t.Job.Customer.Id,
                            t.Job.Customer.Name,
                            t.Job.Customer.Number,
                            t.Job.Customer.OrganizationId
                        }
                    }
                })
                .ToList();

            return Ok(projects);
        }

        // GET: api/Kiosk/InventoryItems/Search
        [HttpGet]
        [Route("api/Kiosk/InventoryItems/Search")]
        [ResponseType(typeof(QBDInventoryItem))]
        public IHttpActionResult SearchInventoryItems([FromUri] string barCodeValue)
        {
            var currentUser = CurrentUser();

            var inventoryItem = _context.QBDInventoryItems
                .Where(i => i.OrganizationId == currentUser.OrganizationId)
                .Where(i => i.BarCodeValue == barCodeValue)
                .FirstOrDefault();

            if (inventoryItem == null)
                return NotFound();

            // Return item with units of measure.
            if (inventoryItem.QBDUnitOfMeasureSetId.HasValue)
            {
                inventoryItem.QBDUnitOfMeasureSet = _context.QBDUnitOfMeasureSets
                    .Where(s => s.Id == inventoryItem.QBDUnitOfMeasureSetId)
                    .FirstOrDefault();

                return Ok(new
                {
                    inventoryItem.Id,
                    inventoryItem.FullName,
                    inventoryItem.BarCodeValue,
                    inventoryItem.ListId,
                    inventoryItem.Name,
                    inventoryItem.ManufacturerPartNumber,
                    inventoryItem.PurchaseCost,
                    inventoryItem.PurchaseDescription,
                    inventoryItem.SalesPrice,
                    inventoryItem.SalesDescription,
                    QBDUnitOfMeasureSet = new
                    {
                        inventoryItem.QBDUnitOfMeasureSet.ListId,
                        inventoryItem.QBDUnitOfMeasureSet.Name,
                        inventoryItem.QBDUnitOfMeasureSet.UnitOfMeasureType,
                        UnitNamesAndAbbreviations = JsonConvert.DeserializeObject<QuickBooksUnitOfMeasures>(inventoryItem.QBDUnitOfMeasureSet.UnitNamesAndAbbreviations)
                    }
                });
            }
            else
            {
                return Ok(new
                {
                    inventoryItem.Id,
                    inventoryItem.FullName,
                    inventoryItem.BarCodeValue,
                    inventoryItem.ListId,
                    inventoryItem.Name,
                    inventoryItem.ManufacturerPartNumber,
                    inventoryItem.PurchaseCost,
                    inventoryItem.PurchaseDescription,
                    inventoryItem.SalesPrice,
                    inventoryItem.SalesDescription
                });
            }
        }

        // GET: api/Kiosk/InventoryItems/Consume
        [HttpPost]
        [Route("api/Kiosk/InventoryItems/Consume")]
        [ResponseType(typeof(QBDInventoryConsumption))]
        public IHttpActionResult ConsumeInventoryItem([FromUri] long qbdInventoryItemId, [FromUri] int quantity, [FromUri] string hostname, [FromUri] string unitOfMeasure = "")
        {
            var currentUser = CurrentUser();
            var inventorySiteEnabled = false;

            // Find the current punch.
            var currentPunch = _context.Punches
                .Where(p => p.UserId == currentUser.Id)
                .Where(p => p.OutAt == null)
                .OrderByDescending(p => p.InAt)
                .FirstOrDefault();

            if (currentPunch == null)
                return BadRequest("Cannot consume inventory without being punched in.");

            long? siteId = null;
            if (inventorySiteEnabled)
            {
                // Inventory site is determined by the hostname.
                var sites = new Dictionary<string, string>()
                {
                    { "MARTIN-01", "" },
                    { "RR01", "" },
                    { "RR08", "" },
                    { "RR09", "" },
                    { "RR11", "" },
                    { "RR12", "" },
                    { "RR23", "" },
                    { "RR24", "" },
                    { "RR25", "" },
                    { "RR26", "" },
                    { "RR27", "" },
                    { "RR28", "" },
                    { "RR17", "" },
                    { "RR16", "" },
                    { "RR18", "" },
                    { "RR19", "" },
                    { "RR02", "" },
                    { "RR03", "" },
                    { "RR05", "" },
                    { "RR06", "" },
                    { "RR04", "" },
                    { "RR15", "" },
                    { "Pocket_PC", "" }
                };
                var siteForHostname = sites[hostname];

                // Ensure there is an inventory site for the given hostname, if necessary.
                if (string.IsNullOrEmpty(siteForHostname))
                    return BadRequest($"No inventory site for hostname {hostname} specified");

                siteId = _context.QBDInventorySites
                    .Where(s => s.FullName == siteForHostname)
                    .Select(s => s.Id)
                    .FirstOrDefault();
            }

            var consumption = new QBDInventoryConsumption()
            {
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = currentUser.Id,
                OrganizationId = currentUser.OrganizationId,
                Quantity = quantity,
                TaskId = currentPunch.TaskId,
                QBDInventoryItemId = qbdInventoryItemId,
                Hostname = hostname,
                QBDInventorySiteId = siteId,
                UnitOfMeasure = unitOfMeasure
            };

            _context.QBDInventoryConsumptions.Add(consumption);
            _context.SaveChanges();

            return Ok(consumption);
        }

        // GET: api/Kiosk/TimeZones
        [HttpGet]
        [Route("api/Kiosk/TimeZones")]
        [ResponseType(typeof(IEnumerable<IanaTimeZone>))]
        public IHttpActionResult TimeZones()
        {
            List<IanaTimeZone> zones = new List<IanaTimeZone>();
            var now = SystemClock.Instance.GetCurrentInstant();
            var tzdb = DateTimeZoneProviders.Tzdb;
            var countryCode = "";

            var list =
                from location in TzdbDateTimeZoneSource.Default.ZoneLocations
                where string.IsNullOrEmpty(countryCode) ||
                      location.CountryCode.Equals(countryCode,
                        StringComparison.OrdinalIgnoreCase)
                let zoneId = location.ZoneId
                let tz = tzdb[zoneId]
                let offset = tz.GetZoneInterval(now).StandardOffset
                orderby offset, zoneId
                select new
                {
                    Id = zoneId,
                    CountryCode = location.CountryCode
                };

            foreach (var z in list)
            {
                zones.Add(new IanaTimeZone() { Id = z.Id, CountryCode = z.CountryCode });
            }

            return Ok(zones);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
                repo.Dispose();
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