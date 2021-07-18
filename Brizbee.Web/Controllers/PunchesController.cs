//
//  PunchesController.cs
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

using Azure.Storage.Blobs;
using Brizbee.Common.Models;
using Brizbee.Web.Repositories;
using Brizbee.Web.Serialization;
using Brizbee.Web.Services;
using Microsoft.AspNet.OData;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;

namespace Brizbee.Web.Controllers
{
    public class PunchesController : BaseODataController
    {
        private SqlContext db = new SqlContext();
        private PunchRepository repo = new PunchRepository();

        // GET: odata/Punches
        [EnableQuery(PageSize = 500, MaxExpansionDepth = 3)]
        public IQueryable<Punch> GetPunches()
        {
            var currentUser = CurrentUser();

            var userIds = db.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id);

            return db.Punches.Where(p => userIds.Contains(p.UserId));
        }

        // GET: odata/Punches(5)
        [EnableQuery]
        public SingleResult<Punch> GetPunch([FromODataUri] int key)
        {
            var currentUser = CurrentUser();

            var userIds = db.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id);

            return SingleResult.Create(db.Punches
                .Where(p => userIds.Contains(p.UserId))
                .Where(p => p.Id == key));
        }

        // POST: odata/Punches
        public IHttpActionResult Post(Punch punch)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUser = CurrentUser();

            // Ensure user is an administrator.
            if (currentUser.Role != "Administrator")
                return BadRequest();

            // Ensure user is in same organization.
            var isValidUserId = db.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Where(u => u.IsDeleted == false)
                .Where(u => u.Id == punch.UserId)
                .Any();

            if (!isValidUserId)
                return BadRequest();

            // Ensure task is in same organization.
            var isValidTaskId = db.Tasks
                .Include(t => t.Job.Customer)
                .Where(t => t.Job.Customer.OrganizationId == currentUser.OrganizationId)
                .Where(t => t.Id == punch.TaskId)
                .Any();

            if (!isValidTaskId)
                return BadRequest();

            // Get the public address and hostname for the punch.
            var sourceIpAddress = HttpContext.Current.Request.UserHostAddress;
            var sourceHostname = HttpContext.Current.Request.UserHostName;
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
            var doesOverlap = db.Punches
                .Include(p => p.Task.Job.Customer)
                .Where(p => p.Task.Job.Customer.OrganizationId == currentUser.OrganizationId)
                .Where(p => p.UserId == punch.UserId)
                .Where(p => p.InAt >= punch.InAt && p.OutAt <= punch.OutAt)
                .Any();

            if (doesOverlap)
                return BadRequest();

            db.Punches.Add(punch);

            db.SaveChanges();

            return Created(punch);
        }

        // PATCH: odata/Punches(5)
        [AcceptVerbs("PATCH", "MERGE")]
        public IHttpActionResult Patch([FromODataUri] int key, Delta<Punch> patch)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUser = CurrentUser();

            // Ensure user is an administrator.
            if (currentUser.Role != "Administrator")
                return BadRequest();

            var punch = db.Punches
                .Include("User")
                .Where(p => p.User.OrganizationId == currentUser.OrganizationId)
                .Where(p => p.Id == key)
                .FirstOrDefault();

            if (punch == null)
                return NotFound();

            // Cannot modify locked punches.
            if (punch.CommitId.HasValue)
                return BadRequest();

            // Do not allow modifying some properties.
            if (patch.GetChangedPropertyNames().Contains("OrganizationId"))
            {
                return BadRequest("Cannont modify OrganizationId");
            }

            // Peform the update
            patch.Patch(punch);

            // Ensure InAt is at bottom of hour and OutAt is at top of the hour
            punch.InAt = new DateTime(punch.InAt.Year, punch.InAt.Month, punch.InAt.Day, punch.InAt.Hour, punch.InAt.Minute, 0, 0);
            if (punch.OutAt.HasValue)
            {
                punch.OutAt = new DateTime(punch.OutAt.Value.Year, punch.OutAt.Value.Month, punch.OutAt.Value.Day, punch.OutAt.Value.Hour, punch.OutAt.Value.Minute, 0, 0);
            }

            db.SaveChanges();

            return Updated(punch);
        }

        // DELETE: odata/Punches(5)
        public IHttpActionResult Delete([FromODataUri] int key)
        {
            var currentUser = CurrentUser();

            var punch = db.Punches
                .Include("User")
                .Where(p => p.Id == key)
                .FirstOrDefault();

            // Ensure user is an administrator in the same organization.
            if (currentUser.Role != "Administrator" ||
                currentUser.OrganizationId != punch.User.OrganizationId)
                return BadRequest();

            // Delete the object itself.
            db.Punches.Remove(punch);

            db.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        // GET: odata/Punches/Default.Current
        [HttpGet]
        [EnableQuery(MaxExpansionDepth = 3)]
        public IQueryable<Punch> Current()
        {
            var userId = CurrentUser().Id;
            return db.Punches
                .Where(p => p.UserId == userId)
                .Where(p => p.OutAt == null)
                .OrderByDescending(p => p.InAt)
                .Take(1);
        }

        // GET: odata/Punches/Default.Download
        [HttpGet]
        public IHttpActionResult Download([FromODataUri] int CommitId)
        {
            var punches = db.Punches
                .Include(p => p.User)
                .Include(p => p.Task.Job.Customer)
                .Include(p => p.PayrollRate)
                .Include(p => p.ServiceRate)
                .Where(p => p.CommitId == CommitId)
                .OrderBy(p => p.InAt)
                .ToList();
            
            return Json(punches);
        }

        // POST: odata/Punches/Default.PunchIn
        [HttpPost]
        public IHttpActionResult PunchIn(ODataActionParameters parameters)
        {
            var taskId = (int)parameters["TaskId"];
            var timezone = (string)parameters["InAtTimeZone"];
            var latitudeForInAt = (string)parameters["LatitudeForInAt"];
            var longitudeForInAt = (string)parameters["LongitudeForInAt"];

            var sourceHardware = (string)parameters["SourceHardware"];
            var sourceHostname = HttpContext.Current.Request.UserHostName;
            var sourceIpAddress = HttpContext.Current.Request.UserHostAddress;
            var sourceOperatingSystem = (string)parameters["SourceOperatingSystem"];
            var sourceOperatingSystemVersion = (string)parameters["SourceOperatingSystemVersion"];
            var sourceBrowser = (string)parameters["SourceBrowser"];
            var sourceBrowserVersion = (string)parameters["SourceBrowserVersion"];

            try
            {
                var punch = repo.PunchIn(
                    taskId,
                    CurrentUser(),
                    "",
                    timezone,
                    latitudeForInAt,
                    longitudeForInAt,
                    sourceHardware,
                    sourceHostname,
                    sourceIpAddress,
                    sourceOperatingSystem,
                    sourceOperatingSystemVersion,
                    sourceBrowser,
                    sourceBrowserVersion);
                return Created(punch);
            }
            catch (DbEntityValidationException e)
            {
                string message = "";

                foreach (var eve in e.EntityValidationErrors)
                {
                    foreach (var ve in eve.ValidationErrors)
                    {
                        message += string.Format("{0} has error '{1}'; ", ve.PropertyName, ve.ErrorMessage);
                    }
                }

                return Content(HttpStatusCode.BadRequest, message);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.BadRequest, ex.Message);
            }
        }

        // POST: odata/Punches/Default.PunchOut
        [HttpPost]
        public IHttpActionResult PunchOut(ODataActionParameters parameters)
        {
            var timezone = (string)parameters["OutAtTimeZone"];
            var latitudeForOutAt = (string)parameters["LatitudeForOutAt"];
            var longitudeForOutAt = (string)parameters["LongitudeForOutAt"];

            var sourceHardware = (string)parameters["SourceHardware"];
            var sourceHostname = HttpContext.Current.Request.UserHostName;
            var sourceIpAddress = HttpContext.Current.Request.UserHostAddress;
            var sourceOperatingSystem = (string)parameters["SourceOperatingSystem"];
            var sourceOperatingSystemVersion = (string)parameters["SourceOperatingSystemVersion"];
            var sourceBrowser = (string)parameters["SourceBrowser"];
            var sourceBrowserVersion = (string)parameters["SourceBrowserVersion"];

            try
            {
                var punch = repo.PunchOut(
                    CurrentUser(),
                    "",
                    timezone,
                    latitudeForOutAt,
                    longitudeForOutAt,
                    sourceHardware,
                    sourceHostname,
                    sourceIpAddress,
                    sourceOperatingSystem,
                    sourceOperatingSystemVersion,
                    sourceBrowser,
                    sourceBrowserVersion);
                return Created(punch);
            }
            catch (DbEntityValidationException e)
            {
                string message = "";

                foreach (var eve in e.EntityValidationErrors)
                {
                    foreach (var ve in eve.ValidationErrors)
                    {
                        message += string.Format("{0} has error '{1}'; ", ve.PropertyName, ve.ErrorMessage);
                    }
                }

                return Content(HttpStatusCode.BadRequest, message);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.BadRequest, ex.Message);
            }
        }

        // POST: odata/Punches/Default.SplitAtMidnight
        [HttpPost]
        public IHttpActionResult SplitAtMidnight(ODataActionParameters parameters)
        {
            var parsedInAt = DateTime.Parse(parameters["InAt"] as string);
            var parsedOutAt = DateTime.Parse(parameters["OutAt"] as string);
            var currentUser = CurrentUser();
            var nowUtc = DateTime.UtcNow;
            var inAt = new DateTime(parsedInAt.Year, parsedInAt.Month, parsedInAt.Day, 0, 0, 0, 0);
            var outAt = new DateTime(parsedOutAt.Year, parsedOutAt.Month, parsedOutAt.Day, 23, 59, 0, 0);
            int[] userIds = db.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id)
                .ToArray();
            var originalPunchesTracked = db.Punches
                .Where(p => userIds.Contains(p.UserId))
                .Where(p => p.InAt >= inAt && p.OutAt.HasValue && p.OutAt.Value <= outAt)
                .Where(p => !p.CommitId.HasValue); // Only uncommited punches
            var originalPunchesNotTracked = originalPunchesTracked
                .AsNoTracking() // Will be manipulated in memory
                .ToList();

            // Save what the punches looked like before.
            var before = originalPunchesNotTracked;

            var splitPunches = new PunchService().SplitAtMidnight(originalPunchesNotTracked, currentUser);

            // Save what the punches look like after.
            var after = splitPunches;

            // Delete the old punches and save the new ones
            db.Punches.RemoveRange(originalPunchesTracked);
            db.SaveChanges();

            db.Punches.AddRange(splitPunches);
            db.SaveChanges();

            try
            {
                // Attempt to save the backup of the punches on Azure.
                var backup = new
                {
                    Before = before,
                    After = after
                };
                var json = JsonConvert.SerializeObject(backup);

                // Prepare to upload the backup.
                var azureConnectionString = ConfigurationManager.AppSettings["PunchBackupsAzureStorageConnectionString"].ToString();
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
                Trace.TraceWarning(ex.ToString());
            }

            return Ok();
        }

        // POST: odata/Punches/Default.PopulateRates
        [HttpPost]
        public IHttpActionResult PopulateRates(ODataActionParameters parameters)
        {
            var populateOptions = parameters["Options"] as PopulateRateOptions;
            var currentUser = CurrentUser();
            var nowUtc = DateTime.UtcNow;
            var inAt = new DateTime(populateOptions.InAt.Year, populateOptions.InAt.Month, populateOptions.InAt.Day, 0, 0, 0, 0, DateTimeKind.Unspecified);
            var outAt = new DateTime(populateOptions.OutAt.Year, populateOptions.OutAt.Month, populateOptions.OutAt.Day, 23, 59, 0, 0, DateTimeKind.Unspecified);
            populateOptions.InAt = inAt;
            populateOptions.OutAt = outAt;
            int[] userIds = db.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id)
                .ToArray();
            var originalPunchesTracked = db.Punches
                .Where(p => userIds.Contains(p.UserId))
                .Where(p => p.InAt >= inAt && p.OutAt.HasValue && p.OutAt.Value <= outAt)
                .Where(p => !p.CommitId.HasValue); // Only uncommited punches
            var originalPunchesNotTracked = originalPunchesTracked
                .AsNoTracking() // Will be manipulated in memory
                .ToList();

            // Save what the punches looked like before.
            var before = originalPunchesNotTracked;

            var populatedPunches = new PunchService().Populate(populateOptions, originalPunchesNotTracked, currentUser);

            // Save what the punches look like after.
            var after = populatedPunches;

            // Delete the old punches and save the new ones.
            db.Punches.RemoveRange(originalPunchesTracked);
            db.SaveChanges();

            db.Punches.AddRange(populatedPunches);
            db.SaveChanges();

            try
            {
                // Attempt to save the backup of the punches on Azure.
                var backup = new
                {
                    Before = before,
                    After = after
                };
                var json = JsonConvert.SerializeObject(backup);

                // Prepare to upload the backup.
                var azureConnectionString = ConfigurationManager.AppSettings["PunchBackupsAzureStorageConnectionString"].ToString();
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
                Trace.TraceWarning(ex.ToString());
            }

            return Ok();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
                repo.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}