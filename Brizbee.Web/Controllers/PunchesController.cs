using Brizbee.Common.Database;
using Brizbee.Common.Models;
using Brizbee.Web.Repositories;
using Brizbee.Web.Serialization;
using Brizbee.Web.Services;
using Microsoft.AspNet.OData;
using System;
using System.Collections;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
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
            return repo.GetAll(CurrentUser());
        }

        // GET: odata/Punches(5)
        [EnableQuery]
        public SingleResult<Punch> GetPunch([FromODataUri] int key)
        {
            return SingleResult.Create(repo.Get(key, CurrentUser()));
        }

        // POST: odata/Punches
        public IHttpActionResult Post(Punch punch)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get the public address and hostname for the punch
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

            punch = repo.Create(punch, CurrentUser());

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

            var punch = repo.Update(key, patch, CurrentUser());

            return Updated(punch);
        }

        // DELETE: odata/Punches(5)
        public IHttpActionResult Delete([FromODataUri] int key)
        {
            repo.Delete(key, CurrentUser());
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

        // POST: odata/Punches/Default.Split
        [HttpPost]
        public IHttpActionResult Split(ODataActionParameters parameters)
        {
            //var splitter = new PunchSplitter();
            //string type = parameters["Type"] as string;
            DateTime inAt = DateTime.Parse(parameters["InAt"] as string);
            DateTime outAt = DateTime.Parse(parameters["OutAt"] as string);
            var currentUser = CurrentUser();
            int[] userIds = db.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id)
                .ToArray();

            //switch (type)
            //{
            //    case "minutes":
            //        int minutes = int.Parse(parameters["Minutes"] as string);
            //        splitter.SplitAtMinutes(userIds, inAt, outAt, minutes, currentUser);
            //        Trace.TraceInformation("Splitting at minutes " + parameters["Minutes"] as string);
            //        break;
            //    case "time":
            //        string time = parameters["Time"] as string;
            //        var separated = time.Split(':');
            //        int hour = int.Parse(separated[0]);
            //        Trace.TraceInformation("Splitting at time " + parameters["Time"] as string);
            //        splitter.SplitAtHour(userIds, inAt, outAt, hour, currentUser);
            //        break;
            //}

            var service = new PunchService();

            var punches = db.Punches
                .Where(p => userIds.Contains(p.UserId))
                .Where(p => p.InAt >= inAt && p.OutAt.HasValue && p.OutAt.Value <= outAt)
                .OrderBy(p => p.UserId)
                .ThenBy(p => p.InAt)
                .ToList();

            var split = service.SplitAtMidnight(punches, currentUser);
            var processed = service.SplitAtMinute(split, currentUser, minuteOfDay: 990);

            db.Punches.RemoveRange(punches);

            db.Punches.AddRange(processed);

            db.SaveChanges();

            return Ok();
        }

        // POST: odata/Punches/Default.SplitAtMidnight
        [HttpPost]
        public IHttpActionResult SplitAtMidnight(ODataActionParameters parameters)
        {
            DateTime parsedInAt = DateTime.Parse(parameters["InAt"] as string);
            DateTime parsedOutAt = DateTime.Parse(parameters["OutAt"] as string);
            User currentUser = CurrentUser();
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

            var splitPunches = new PunchService().SplitAtMidnight(originalPunchesNotTracked, currentUser);

            // Delete the old punches and save the new ones
            db.Punches.RemoveRange(originalPunchesTracked);
            db.SaveChanges();

            db.Punches.AddRange(splitPunches);
            db.SaveChanges();

            return Ok();
        }

        // POST: odata/Punches/Default.PopulateRates
        [HttpPost]
        public IHttpActionResult PopulateRates(ODataActionParameters parameters)
        {
            var populateOptions = parameters["Options"] as PopulateRateOptions;
            var currentUser = CurrentUser();
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

            var populatedPunches = new PunchService().Populate(populateOptions, originalPunchesNotTracked, currentUser);

            // Delete the old punches and save the new ones
            db.Punches.RemoveRange(originalPunchesTracked);
            db.SaveChanges();

            db.Punches.AddRange(populatedPunches);
            db.SaveChanges();

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