﻿//
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

using Brizbee.Api.Repositories;
using Brizbee.Api.Serialization.Expanded;
using Brizbee.Core.Models;
using Brizbee.Core.Serialization;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NodaTime;
using NodaTime.TimeZones;
using System.Diagnostics;
using System.Text.Json;

namespace Brizbee.Api.Controllers
{
    public class KioskController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly SqlContext _context;
        private readonly IMemoryCache _memoryCache;

        public KioskController(IConfiguration configuration, SqlContext context, IMemoryCache memoryCache)
        {
            _configuration = configuration;
            _context = context;
            _memoryCache = memoryCache;
        }

        // POST: api/Kiosk/PunchIn
        [HttpPost("api/Kiosk/PunchIn")]
        [ProducesResponseType(typeof(Punch), StatusCodes.Status200OK)]
        public IActionResult PunchIn([FromQuery] int taskId, [FromQuery] string timeZone,
            [FromQuery] string latitude, [FromQuery] string longitude, [FromQuery] string sourceHardware,
            [FromQuery] string sourceOperatingSystem, [FromQuery] string sourceOperatingSystemVersion,
            [FromQuery] string sourceBrowser, [FromQuery] string sourceBrowserVersion)
        {
            var sourceHostname = ""; // Leave blank

            // Attempt to get the client IP address.
            var sourceIpAddress = $"{HttpContext.Connection.RemoteIpAddress}";

            var currentUser = CurrentUser();

            var task = _context.Tasks!
                .Include(t => t.Job!.Customer)
                .Where(t => t.Job!.Customer!.OrganizationId == currentUser.OrganizationId)
                .FirstOrDefault(t => t.Id == taskId);

            // Ensure that task was found.
            if (task == null) return NotFound();
            
            // Ensure job is open.
            if (!new [] { "Open", "Proposed" }.Contains(task.Job!.Status))
                return BadRequest("Cannot punch in on tasks for projects that are not open or proposed.");

            // Prevent double submission.
            var submission = _memoryCache.Get($"submission.punchin.{currentUser.Id}") as bool?;
            if (submission.HasValue)
                return BadRequest("Cannot punch in twice within 5 seconds.");

            try
            {
                var repo = new PunchRepository(_context);

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
                _memoryCache.Set($"submission.punchin.{currentUser.Id}", true, DateTime.UtcNow.AddSeconds(5));

                return Ok(punch);
            }
            catch (DbUpdateException ex)
            {
                Trace.TraceError(ex.ToString());
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                return BadRequest(new { Message = ex.Message });
            }
        }

        // POST: api/Kiosk/PunchOut
        [HttpPost("api/Kiosk/PunchOut")]
        [ProducesResponseType(typeof(Punch), StatusCodes.Status200OK)]
        public IActionResult PunchOut([FromQuery] string timeZone,
            [FromQuery] string latitude, [FromQuery] string longitude, [FromQuery] string sourceHardware,
            [FromQuery] string sourceOperatingSystem, [FromQuery] string sourceOperatingSystemVersion,
            [FromQuery] string sourceBrowser, [FromQuery] string sourceBrowserVersion)
        {
            var sourceHostname = ""; // Leave blank

            // Attempt to get the client IP address.
            var sourceIpAddress = $"{HttpContext.Connection.RemoteIpAddress}";

            var currentUser = CurrentUser();

            // Prevent double submission.
            var submission = _memoryCache.Get($"submission.punchout.{currentUser.Id}") as bool?;
            if (submission.HasValue)
                return BadRequest("Cannot punch out twice within 5 seconds.");

            try
            {
                var repo = new PunchRepository(_context);

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
                _memoryCache.Set($"submission.punchout.{currentUser.Id}", true, DateTime.UtcNow.AddSeconds(5));

                return Ok(punch);
            }
            catch (DbUpdateException ex)
            {
                Trace.TraceError(ex.ToString());
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                return BadRequest(ex.Message);
            }
        }

        // GET: api/Kiosk/Punches/Current
        [HttpGet("api/Kiosk/Punches/Current")]
        [ProducesResponseType(typeof(Punch), StatusCodes.Status200OK)]
        public IActionResult CurrentPunch()
        {
            User currentUser = CurrentUser();

            Punch currentPunch = null;
            using (var connection = new SqlConnection(_configuration.GetConnectionString("SqlContext")))
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
                        Task = new Brizbee.Core.Models.Task()
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
        [HttpPost("api/Kiosk/TimeCard")]
        [ProducesResponseType(typeof(TimesheetEntry), StatusCodes.Status200OK)]
        public IActionResult TimeCard([FromQuery] int taskId, [FromQuery] DateTime enteredAt, [FromQuery] int minutes, [FromQuery] string notes = "")
        {
            var currentUser = CurrentUser();

            var task = _context.Tasks
                .Include(t => t.Job!.Customer)
                .Where(t => t.Job!.Customer!.OrganizationId == currentUser.OrganizationId)
                .Where(t => t.Id == taskId)
                .FirstOrDefault();

            // Ensure that task was found.
            if (task == null) return NotFound();

            // Ensure job is open.
            if (task.Job!.Status != "Open")
                return BadRequest("Cannot add time cards for projects that are not open.");

            try
            {
                var timesheetEntry = new TimesheetEntry()
                {
                    CreatedAt = DateTime.UtcNow,
                    EnteredAt = enteredAt,
                    Minutes = minutes,
                    Notes = notes,
                    TaskId = taskId,
                    UserId = currentUser.Id
                };
                _context.TimesheetEntries.Add(timesheetEntry);
                _context.SaveChanges();

                return Ok(timesheetEntry);
            }
            catch (DbUpdateException ex)
            {
                Trace.TraceError(ex.ToString());
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                return BadRequest(new { Message = ex.Message });
            }
        }

        // GET: api/Kiosk/SearchTasks
        [HttpGet("api/Kiosk/SearchTasks")]
        [ProducesResponseType(typeof(Brizbee.Core.Models.Task), StatusCodes.Status200OK)]
        public IActionResult SearchTasks([FromQuery] string taskNumber)
        {
            var currentUser = CurrentUser();

            // Search only tasks that belong to customers in the organization.
            var task = _context.Tasks
                .Include("Job")
                .Include("Job.Customer")
                .Where(t => t.Job!.Customer!.OrganizationId == currentUser.OrganizationId)
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
                        t.Job!.Id,
                        t.Job.Name,
                        t.Job.Number,
                        t.Job.CreatedAt,
                        t.Job.CustomerId,
                        t.Job.Status,
                        Customer = new
                        {
                            t.Job!.Customer!.Id,
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
        [HttpGet("api/Kiosk/Customers")]
        [ProducesResponseType(typeof(IEnumerable<Customer>), StatusCodes.Status200OK)]
        public IActionResult Customers()
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
        [HttpGet("api/Kiosk/Projects")]
        [ProducesResponseType(typeof(IEnumerable<Job>), StatusCodes.Status200OK)]
        public IActionResult Projects([FromQuery] int customerId)
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
        [HttpGet("api/Kiosk/Tasks")]
        [ProducesResponseType(typeof(IEnumerable<Brizbee.Core.Models.Task>), StatusCodes.Status200OK)]
        public IActionResult Tasks([FromQuery] int projectId)
        {
            var currentUser = CurrentUser();

            var projects = _context.Tasks
                .Include(t => t.Job!.Customer)
                .Where(t => t.Job!.Customer!.OrganizationId == currentUser.OrganizationId)
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
                        t.Job!.CreatedAt,
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
                            t.Job!.Customer!.CreatedAt,
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
        [HttpGet("api/Kiosk/InventoryItems/Search")]
        [ProducesResponseType(typeof(QBDInventoryItem), StatusCodes.Status200OK)]
        public IActionResult SearchInventoryItems([FromQuery] string barCodeValue)
        {
            var currentUser = CurrentUser();

            // Attempt to find an item based on the custom bar code
            var foundCustomItem = _context.QBDInventoryItems
                .Where(i => i.OrganizationId == currentUser.OrganizationId)
                .Where(i => i.CustomBarCodeValue == barCodeValue)
                .FirstOrDefault();

            // Attempt to find an item based on the QuickBooks Enterprise bar code
            var foundQuickBooksItem = _context.QBDInventoryItems
                .Where(i => i.OrganizationId == currentUser.OrganizationId)
                .Where(i => i.BarCodeValue == barCodeValue)
                .FirstOrDefault();

            if (foundCustomItem == null && foundQuickBooksItem == null)
                return NotFound();

            // Determine which item will be returned
            var chosenBarCodeValue = foundCustomItem != null ? foundCustomItem.CustomBarCodeValue : foundQuickBooksItem.BarCodeValue;
            var chosenItem = foundCustomItem != null ? foundCustomItem : foundQuickBooksItem;

            // Return item with units of measure
            if (chosenItem.QBDUnitOfMeasureSetId.HasValue)
            {
                chosenItem.QBDUnitOfMeasureSet = _context.QBDUnitOfMeasureSets
                    .Where(s => s.Id == chosenItem.QBDUnitOfMeasureSetId)
                    .FirstOrDefault();

                return Ok(new
                {
                    chosenItem.Id,
                    chosenItem.FullName,
                    BarCodeValue = chosenBarCodeValue,
                    chosenItem.ListId,
                    chosenItem.Name,
                    chosenItem.ManufacturerPartNumber,
                    chosenItem.PurchaseCost,
                    chosenItem.PurchaseDescription,
                    chosenItem.SalesPrice,
                    chosenItem.SalesDescription,
                    QBDUnitOfMeasureSet = new
                    {
                        chosenItem.QBDUnitOfMeasureSet.ListId,
                        chosenItem.QBDUnitOfMeasureSet.Name,
                        chosenItem.QBDUnitOfMeasureSet.UnitOfMeasureType,
                        UnitNamesAndAbbreviations = JsonSerializer.Deserialize<QuickBooksUnitOfMeasures>(chosenItem.QBDUnitOfMeasureSet.UnitNamesAndAbbreviations)
                    }
                });
            }
            else
            {
                return Ok(new
                {
                    chosenItem.Id,
                    chosenItem.FullName,
                    BarCodeValue = chosenBarCodeValue,
                    chosenItem.ListId,
                    chosenItem.Name,
                    chosenItem.ManufacturerPartNumber,
                    chosenItem.PurchaseCost,
                    chosenItem.PurchaseDescription,
                    chosenItem.SalesPrice,
                    chosenItem.SalesDescription
                });
            }
        }

        // GET: api/Kiosk/InventoryItems/Consume
        [HttpPost("api/Kiosk/InventoryItems/Consume")]
        [ProducesResponseType(typeof(QBDInventoryConsumption), StatusCodes.Status200OK)]
        public IActionResult ConsumeInventoryItem([FromQuery] long qbdInventoryItemId, [FromQuery] int quantity, [FromQuery] string hostname, [FromQuery] string unitOfMeasure = "")
        {
            var currentUser = CurrentUser();
            const bool inventorySiteEnabled = false;

            // Find the current punch.
            var currentPunch = _context.Punches
                .Where(p => p.UserId == currentUser.Id)
                .Where(p => p.OutAt == null)
                .OrderByDescending(p => p.InAt)
                .FirstOrDefault();

            if (currentPunch == null)
                return BadRequest("Cannot consume inventory without being punched in.");

            if (quantity < 1)
                return BadRequest("Cannot consume inventory with negative or zero quantity.");

            long? siteId = null;
            if (inventorySiteEnabled)
            {
                // Inventory site is determined by the hostname.
                var sites = new Dictionary<string, string>()
                {
                    { "MARTIN-01", "" }
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

            _context.QBDInventoryConsumptions!.Add(consumption);
            _context.SaveChanges();

            return Ok(consumption);
        }

        // GET: api/Kiosk/TimeZones
        [HttpGet("api/Kiosk/TimeZones")]
        [ProducesResponseType(typeof(IEnumerable<IanaTimeZone>), StatusCodes.Status200OK)]
        public IActionResult TimeZones()
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

        private User CurrentUser()
        {
            var type = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
            var sub = HttpContext.User.Claims.FirstOrDefault(c => c.Type == type).Value;
            var currentUserId = int.Parse(sub);
            return _context.Users
                .Include(u => u.Organization)
                .Where(u => u.Id == currentUserId)
                .FirstOrDefault();
        }
    }
}