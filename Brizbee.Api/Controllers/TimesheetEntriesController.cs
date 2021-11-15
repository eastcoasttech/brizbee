//
//  TimesheetEntriesController.cs
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

using Brizbee.Core.Models;
using Dapper;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Brizbee.Api.Controllers
{
    public class TimesheetEntriesController : ODataController
    {
        private readonly IConfiguration _configuration;
        private readonly SqlContext _context;
        private readonly TelemetryClient _telemetryClient;

        public TimesheetEntriesController(IConfiguration configuration, SqlContext context, TelemetryClient telemetry)
        {
            _configuration = configuration;
            _context = context;
            _telemetryClient = telemetry;
        }

        // GET: odata/TimesheetEntries
        [EnableQuery(PageSize = 20, MaxExpansionDepth = 3)]
        public IQueryable<TimesheetEntry> GetTimesheetEntries()
        {
            var currentUser = CurrentUser();

            var userIds = _context.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id);

            return _context.TimesheetEntries.Where(p => userIds.Contains(p.UserId));
        }

        // GET: odata/TimesheetEntries(5)
        [EnableQuery]
        public SingleResult<TimesheetEntry> GetTimesheetEntry([FromODataUri] int key)
        {
            var currentUser = CurrentUser();

            var userIds = _context.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id);

            return SingleResult.Create(_context.TimesheetEntries
                .Where(p => userIds.Contains(p.UserId))
                .Where(p => p.Id == key));
        }

        // POST: odata/TimesheetEntries
        public IActionResult Post([FromBody] TimesheetEntry timesheetEntry)
        {
            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanCreateTimecards)
                return Forbid();

            // Auto-generated.
            timesheetEntry.CreatedAt = DateTime.UtcNow;

            // Validate the model.
            ModelState.ClearValidationState(nameof(timesheetEntry));
            if (!TryValidateModel(timesheetEntry, nameof(timesheetEntry)))
                return BadRequest();

            _context.TimesheetEntries.Add(timesheetEntry);

            _context.SaveChanges();

            // Record the activity.
            AuditTimesheetEntry(timesheetEntry.Id, null, JsonSerializer.Serialize(timesheetEntry), currentUser, "CREATE");

            return Ok(timesheetEntry);
        }

        // PATCH: odata/TimesheetEntries(5)
        public IActionResult Patch([FromODataUri] int key, Delta<TimesheetEntry> patch)
        {
            var currentUser = CurrentUser();

            var timesheetEntry = _context.TimesheetEntries
                .Include("User")
                .Where(t => t.Id == key)
                .FirstOrDefault();

            // Ensure that object was found.
            if (timesheetEntry == null) return NotFound();

            // Ensure that user is authorized.
            if (!currentUser.CanModifyTimecards ||
                currentUser.OrganizationId != timesheetEntry.User.OrganizationId)
                return Forbid();

            // Do not allow modifying some properties.
            if (patch.GetChangedPropertyNames().Contains("CreatedAt") ||
                patch.GetChangedPropertyNames().Contains("Id"))
            {
                return BadRequest("Not authorized to modify CreatedAt or Id.");
            }

            // Record the object before any changes are made.
            var before = JsonSerializer.Serialize(timesheetEntry);

            // Peform the update
            patch.Patch(timesheetEntry);

            // Validate the model.
            ModelState.ClearValidationState(nameof(timesheetEntry));
            if (!TryValidateModel(timesheetEntry, nameof(timesheetEntry)))
                return BadRequest();

            _context.SaveChanges();

            // Record the activity.
            AuditTimesheetEntry(timesheetEntry.Id, before, JsonSerializer.Serialize(timesheetEntry), currentUser, "UPDATE");

            return NoContent();
        }

        // DELETE: odata/TimesheetEntries(5)
        public IActionResult Delete([FromODataUri] int key)
        {
            var currentUser = CurrentUser();

            var timesheetEntry = _context.TimesheetEntries
                .Include("User")
                .Where(t => t.Id == key)
                .FirstOrDefault();

            // Ensure that object was found.
            if (timesheetEntry == null) return NotFound();

            // Ensure that user is authorized.
            if (!currentUser.CanDeleteTimecards ||
                currentUser.OrganizationId != timesheetEntry.User.OrganizationId)
                return Forbid();

            // Delete the object itself
            _context.TimesheetEntries.Remove(timesheetEntry);

            _context.SaveChanges();

            // Record the activity.
            AuditTimesheetEntry(timesheetEntry.Id, JsonSerializer.Serialize(timesheetEntry), null, currentUser, "DELETE");

            return NoContent();
        }

        private void AuditTimesheetEntry(int id, string? before, string? after, User currentUser, string action)
        {
            try
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("SqlContext")))
                {
                    string insertQuery = @"
                        INSERT INTO [TimeCardAudits]
                            ([CreatedAt], [ObjectId], [OrganizationId], [UserId], [Action], [Before], [After])
                        VALUES
                            (@CreatedAt, @ObjectId, @OrganizationId, @UserId, @Action, @Before, @After);";

                    var result = connection.Execute(insertQuery, new
                    {
                        CreatedAt = DateTime.UtcNow,
                        ObjectId = id,
                        OrganizationId = currentUser.OrganizationId,
                        UserId = currentUser.Id,
                        Action = action,
                        Before = before,
                        After = after
                    });
                }
            }
            catch (Exception ex)
            {
                _telemetryClient.TrackException(ex);
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
}