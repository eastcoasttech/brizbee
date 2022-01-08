//
//  PunchesController.cs
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

using Azure.Storage.Blobs;
using Brizbee.Api.Serialization;
using Brizbee.Api.Services;
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
using Microsoft.Extensions.Caching.Memory;
using System.Text;
using System.Text.Json;

namespace Brizbee.Api.Controllers
{
    public class PunchesController : ODataController
    {
        private readonly IConfiguration _configuration;
        private readonly SqlContext _context;
        private readonly IMemoryCache _memoryCache;
        private readonly TelemetryClient _telemetryClient;

        public PunchesController(IConfiguration configuration, SqlContext context, IMemoryCache memoryCache, TelemetryClient telemetryClient)
        {
            _configuration = configuration;
            _context = context;
            _memoryCache = memoryCache;
            _telemetryClient = telemetryClient;
        }

        // GET: odata/Punches
        [HttpGet]
        [EnableQuery(PageSize = 500, MaxExpansionDepth = 3)]
        public IQueryable<Punch> GetPunches()
        {
            var currentUser = CurrentUser();

            return _context.Punches
                .Include(p => p.User)
                .Where(p => p.User!.OrganizationId == currentUser.OrganizationId)
                .Where(p => p.User!.IsDeleted == false);
        }

        // GET: odata/Punches(5)
        [HttpGet]
        [EnableQuery]
        public SingleResult<Punch> GetPunch([FromODataUri] int key)
        {
            var currentUser = CurrentUser();

            return SingleResult.Create(_context.Punches
                .Include(p => p.User)
                .Where(p => p.User!.OrganizationId == currentUser.OrganizationId)
                .Where(p => p.User!.IsDeleted == false)
                .Where(p => p.Id == key));
        }

        // POST: odata/Punches
        public IActionResult Post([FromBody] Punch punch)
        {
            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanCreatePunches)
                return Forbid();

            // Ensure user is in same organization.
            var isValidUserId = _context.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Where(u => u.IsDeleted == false)
                .Where(u => u.Id == punch.UserId)
                .Any();

            if (!isValidUserId)
                return BadRequest("You must specify a valid user.");

            // Ensure task is in same organization.
            var isValidTaskId = _context.Tasks
                .Include(t => t.Job.Customer)
                .Where(t => t.Job.Customer.OrganizationId == currentUser.OrganizationId)
                .Where(t => t.Id == punch.TaskId)
                .Any();

            if (!isValidTaskId)
                return BadRequest("You must specify a valid task.");

            // Get the public address and hostname for the punch.
            string sourceIpAddress = "UNKNOWN";
            if (HttpContext.Connection.RemoteIpAddress != null)
                sourceIpAddress = HttpContext.Connection.RemoteIpAddress.ToString();
            var sourceHostname = HttpContext.Request.Host.ToString();
            punch.InAtSourceHostname = sourceHostname;
            punch.InAtSourceIpAddress = sourceIpAddress;
            punch.InAtSourceHardware = "Dashboard"; // Punches created this way are always dashboard
            if (punch.OutAt.HasValue)
            {
                punch.OutAtSourceHostname = sourceHostname;
                punch.OutAtSourceIpAddress = sourceIpAddress;
                punch.OutAtSourceHardware = "Dashboard"; // Punches created this way are always dashboard
            }

            // Auto-generated.
            punch.CreatedAt = DateTime.UtcNow;
            punch.Guid = Guid.NewGuid();

            // Ensure InAt is at bottom of hour and OutAt is at top of the hour.
            punch.InAt = new DateTime(punch.InAt.Year, punch.InAt.Month, punch.InAt.Day, punch.InAt.Hour, punch.InAt.Minute, 0, 0);
            if (punch.OutAt.HasValue)
            {
                punch.OutAt = new DateTime(punch.OutAt.Value.Year, punch.OutAt.Value.Month, punch.OutAt.Value.Day, punch.OutAt.Value.Hour, punch.OutAt.Value.Minute, 0, 0);
            }

            // Ensure punch does not overlap existing punches.
            var doesOverlap = _context.Punches
                .Include(p => p.Task.Job.Customer)
                .Where(p => p.Task.Job.Customer.OrganizationId == currentUser.OrganizationId)
                .Where(p => p.UserId == punch.UserId)
                .Where(p => p.InAt >= punch.InAt && p.OutAt <= punch.OutAt)
                .Any();

            if (doesOverlap)
                return BadRequest("This punch overlaps with another punch.");

            // Validate the model.
            ModelState.ClearValidationState(nameof(punch));
            if (!TryValidateModel(punch, nameof(punch)))
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

            _context.Punches.Add(punch);

            _context.SaveChanges();

            // Record the activity.
            AuditPunch(punch.Id, null, JsonSerializer.Serialize(punch), currentUser, "CREATE");

            return Created("", punch);
        }

        // PATCH: odata/Punches(5)
        public IActionResult Patch([FromODataUri] int key, Delta<Punch> patch)
        {
            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanModifyPunches)
                return Forbid();

            var punch = _context.Punches
                .Include(p => p.User)
                .Where(p => p.User!.OrganizationId == currentUser.OrganizationId)
                .Where(p => p.Id == key)
                .FirstOrDefault();

            // Record the object before any changes are made.
            var before = JsonSerializer.Serialize(punch);

            // Ensure that object was found.
            if (punch == null) return NotFound();

            // Cannot modify locked punches.
            if (punch.CommitId.HasValue)
                return BadRequest();

            // Do not allow modifying some properties.
            if (patch.GetChangedPropertyNames().Contains("OrganizationId") ||
                patch.GetChangedPropertyNames().Contains("CreatedAt") ||
                patch.GetChangedPropertyNames().Contains("Id"))
            {
                return BadRequest("Cannont modify OrganizationId, CreatedAt, and Id.");
            }

            // Peform the update
            patch.Patch(punch);

            // Ensure InAt is at bottom of hour and OutAt is at top of the hour
            punch.InAt = new DateTime(punch.InAt.Year, punch.InAt.Month, punch.InAt.Day, punch.InAt.Hour, punch.InAt.Minute, 0, 0);
            if (punch.OutAt.HasValue)
            {
                punch.OutAt = new DateTime(punch.OutAt.Value.Year, punch.OutAt.Value.Month, punch.OutAt.Value.Day, punch.OutAt.Value.Hour, punch.OutAt.Value.Minute, 0, 0);
            }

            // Validate the model.
            ModelState.ClearValidationState(nameof(punch));
            if (!TryValidateModel(punch, nameof(punch)))
                return BadRequest();

            _context.SaveChanges();

            // Record the activity.
            AuditPunch(punch.Id, before, JsonSerializer.Serialize(punch), currentUser, "UPDATE");

            return NoContent();
        }

        // DELETE: odata/Punches(5)
        public IActionResult Delete([FromODataUri] int key)
        {
            var currentUser = CurrentUser();

            var punch = _context.Punches
                .Include(p => p.User)
                .Where(p => p.User!.OrganizationId == currentUser.OrganizationId)
                .Where(p => p.Id == key)
                .FirstOrDefault();

            // Ensure that object was found.
            if (punch == null) return NotFound();

            // Ensure that user is authorized.
            if (!currentUser.CanDeletePunches)
                return Forbid();

            // Delete the object itself.
            _context.Punches.Remove(punch);

            _context.SaveChanges();

            // Record the activity.
            AuditPunch(punch.Id, JsonSerializer.Serialize(punch), null, currentUser, "DELETE");

            return NoContent();
        }

        // GET: odata/Punches/Download
        [HttpGet]
        public IActionResult Download([FromODataUri] int CommitId)
        {
            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanViewPunches)
                return Forbid();

            var punches = _context.Punches
                .Include(p => p.User)
                .Include(p => p.Task.Job.Customer)
                .Include(p => p.PayrollRate)
                .Include(p => p.ServiceRate)
                .Where(p => p.CommitId == CommitId)
                .OrderBy(p => p.InAt)
                .ToList();

            return new JsonResult(punches);
        }

        // POST: odata/Punches/SplitAtMidnight
        [HttpPost]
        public IActionResult SplitAtMidnight(ODataActionParameters parameters)
        {
            _telemetryClient.TrackEvent("Split:Requested");

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var parsedInAt = DateTime.Parse(parameters["InAt"] as string);
                    var parsedOutAt = DateTime.Parse(parameters["OutAt"] as string);
                    var currentUser = CurrentUser();
                    var nowUtc = DateTime.UtcNow;
                    var inAt = new DateTime(parsedInAt.Year, parsedInAt.Month, parsedInAt.Day, 0, 0, 0, 0);
                    var outAt = new DateTime(parsedOutAt.Year, parsedOutAt.Month, parsedOutAt.Day, 23, 59, 0, 0);
                    int[] userIds = _context.Users
                        .Where(u => u.OrganizationId == currentUser.OrganizationId)
                        .Select(u => u.Id)
                        .ToArray();
                    var originalPunchesTracked = _context.Punches
                        .Where(p => userIds.Contains(p.UserId))
                        .Where(p => p.OutAt.HasValue)
                        .Where(p => p.InAt.Date >= inAt.Date)
                        .Where(p => p.InAt.Date <= outAt.Date)
                        .Where(p => !p.CommitId.HasValue); // Only unlocked punches
                    var originalPunchesNotTracked = originalPunchesTracked
                        .AsNoTracking() // Will be manipulated in memory
                        .ToList();

                    _telemetryClient.TrackEvent("Split:Starting", new Dictionary<string, string>()
                    {
                        { "InAt", inAt.ToString("G") },
                        { "OutAt", outAt.ToString("G") },
                        { "UserId", currentUser.Id.ToString() },
                        { "OrganizationId", currentUser.OrganizationId.ToString() }
                    });

                    // Save what the punches looked like before.
                    var before = originalPunchesNotTracked;

                    var splitPunches = new PunchService(_context).SplitAtMidnight(originalPunchesNotTracked, currentUser);

                    // Save what the punches look like after.
                    var after = splitPunches;

                    // Delete the old punches and save the new ones
                    _context.Punches.RemoveRange(originalPunchesTracked);
                    _context.SaveChanges();

                    _context.Punches.AddRange(splitPunches);
                    _context.SaveChanges();

                    try
                    {
                        // Attempt to save the backup of the punches on Azure.
                        var backup = new
                        {
                            Before = before,
                            After = after
                        };
                        var json = JsonSerializer.Serialize(backup);

                        // Prepare to upload the backup.
                        var azureConnectionString = _configuration["PunchBackupsAzureStorageConnectionString"];
                        BlobServiceClient blobServiceClient = new BlobServiceClient(azureConnectionString);
                        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("split-punch-backups");
                        BlobClient blobClient = containerClient.GetBlobClient($"{currentUser.OrganizationId}/{nowUtc.Ticks}.json");

                        // Perform the upload.
                        using (var stream = new MemoryStream(Encoding.Default.GetBytes(json), false))
                        {
                            blobClient.Upload(stream);
                        }
                    }
                    catch (Exception ex)
                    {
                        _telemetryClient.TrackException(ex);
                    }

                    _telemetryClient.TrackEvent("Split:Succeeded");
                    _telemetryClient.Flush();

                    transaction.Commit();

                    return Ok();
                }
                catch (Exception ex)
                {
                    _telemetryClient.TrackEvent("Split:Failed");
                    _telemetryClient.TrackException(ex);
                    _telemetryClient.Flush();

                    transaction.Rollback();

                    return BadRequest(ex.ToString());
                }
            }
        }

        // POST: odata/Punches/PopulateRates
        [HttpPost]
        public IActionResult PopulateRates(ODataActionParameters parameters)
        {
            _telemetryClient.TrackEvent("Populate:Requested");

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var populateOptions = parameters["Options"] as PopulateRateOptions;
                    var currentUser = CurrentUser();
                    var nowUtc = DateTime.UtcNow;
                    var inAt = new DateTime(populateOptions.InAt.Year, populateOptions.InAt.Month, populateOptions.InAt.Day, 0, 0, 0, 0, DateTimeKind.Unspecified);
                    var outAt = new DateTime(populateOptions.OutAt.Year, populateOptions.OutAt.Month, populateOptions.OutAt.Day, 23, 59, 0, 0, DateTimeKind.Unspecified);
                    populateOptions.InAt = inAt;
                    populateOptions.OutAt = outAt;
                    int[] userIds = _context.Users
                        .Where(u => u.OrganizationId == currentUser.OrganizationId)
                        .Select(u => u.Id)
                        .ToArray();
                    var originalPunchesTracked = _context.Punches
                        .Where(p => userIds.Contains(p.UserId))
                        .Where(p => p.OutAt.HasValue)
                        .Where(p => p.InAt.Date >= inAt.Date)
                        .Where(p => p.InAt.Date <= outAt.Date)
                        .Where(p => !p.CommitId.HasValue); // Only unlocked punches
                    var originalPunchesNotTracked = originalPunchesTracked
                        .AsNoTracking() // Will be manipulated in memory
                        .ToList();

                    _telemetryClient.TrackEvent("Populate:Starting", new Dictionary<string, string>()
                    {
                        { "InAt", inAt.ToString("G") },
                        { "OutAt", outAt.ToString("G") },
                        { "UserId", currentUser.Id.ToString() },
                        { "OrganizationId", currentUser.OrganizationId.ToString() }
                    });

                    // Save what the punches looked like before.
                    var before = originalPunchesNotTracked;

                    var populatedPunches = new PunchService(_context).Populate(populateOptions, originalPunchesNotTracked, currentUser);

                    // Save what the punches look like after.
                    var after = populatedPunches;

                    // Delete the old punches and save the new ones.
                    _context.Punches.RemoveRange(originalPunchesTracked);
                    _context.SaveChanges();

                    _context.Punches.AddRange(populatedPunches);
                    _context.SaveChanges();

                    try
                    {
                        // Attempt to save the backup of the punches on Azure.
                        var backup = new
                        {
                            Before = before,
                            After = after
                        };
                        var json = JsonSerializer.Serialize(backup);

                        // Prepare to upload the backup.
                        var azureConnectionString = _configuration["PunchBackupsAzureStorageConnectionString"];
                        BlobServiceClient blobServiceClient = new BlobServiceClient(azureConnectionString);
                        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("populate-punch-backups");
                        BlobClient blobClient = containerClient.GetBlobClient($"{currentUser.OrganizationId}/{nowUtc.Ticks}.json");

                        // Perform the upload.
                        using (var stream = new MemoryStream(Encoding.Default.GetBytes(json), false))
                        {
                            blobClient.Upload(stream);
                        }
                    }
                    catch (Exception ex)
                    {
                        _telemetryClient.TrackException(ex);
                    }

                    _telemetryClient.TrackEvent("Populate:Succeeded");
                    _telemetryClient.Flush();

                    transaction.Commit();

                    return Ok();
                }
                catch (Exception ex)
                {
                    _telemetryClient.TrackEvent("Populate:Failed");
                    _telemetryClient.TrackException(ex);
                    _telemetryClient.Flush();

                    transaction.Rollback();

                    return BadRequest(ex.ToString());
                }
            }
        }

        private void AuditPunch(int id, string before, string after, User currentUser, string action)
        {
            try
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("SqlContext")))
                {
                    string insertQuery = @"
                        INSERT INTO [PunchAudits]
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
