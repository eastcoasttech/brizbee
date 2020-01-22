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
        private BrizbeeWebContext db = new BrizbeeWebContext();
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
            var source = (string)parameters["SourceForInAt"];
            var timezone = (string)parameters["InAtTimeZone"];
            var latitudeForInAt = (string)parameters["LatitudeForInAt"];
            var longitudeForInAt = (string)parameters["LongitudeForInAt"];

            var sourceHardware = (string)parameters["SourceHardware"];
            var sourceHostname = (string)parameters["SourceHostname"];
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
                    source,
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
            var source = (string)parameters["SourceForOutAt"];
            var timezone = (string)parameters["OutAtTimeZone"];
            var latitudeForOutAt = (string)parameters["LatitudeForOutAt"];
            var longitudeForOutAt = (string)parameters["LongitudeForOutAt"];

            var sourceHardware = (string)parameters["SourceHardware"];
            var sourceHostname = (string)parameters["SourceHostname"];
            var sourceIpAddress = HttpContext.Current.Request.UserHostAddress;
            var sourceOperatingSystem = (string)parameters["SourceOperatingSystem"];
            var sourceOperatingSystemVersion = (string)parameters["SourceOperatingSystemVersion"];
            var sourceBrowser = (string)parameters["SourceBrowser"];
            var sourceBrowserVersion = (string)parameters["SourceBrowserVersion"];

            try
            {
                var punch = repo.PunchOut(
                    CurrentUser(),
                    source,
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
            var splitter = new PunchSplitter();
            string type = parameters["Type"] as string;
            DateTime inAt = DateTime.Parse(parameters["InAt"] as string);
            Trace.TraceInformation(inAt.ToString("yyyy-MM-dd HH:mm:ss"));
            DateTime outAt = DateTime.Parse(parameters["OutAt"] as string);
            Trace.TraceInformation(outAt.ToString("yyyy-MM-dd HH:mm:ss"));
            var currentUser = CurrentUser();
            int[] userIds = db.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id)
                .ToArray();

            switch (type)
            {
                case "minutes":
                    int minutes = int.Parse(parameters["Minutes"] as string);
                    splitter.SplitAtMinutes(userIds, inAt, outAt, minutes, currentUser);
                    Trace.TraceInformation("Splitting at minutes " + parameters["Minutes"] as string);
                    break;
                case "time":
                    string time = parameters["Time"] as string;
                    var separated = time.Split(':');
                    int hour = int.Parse(separated[0]);
                    Trace.TraceInformation("Splitting at time " + parameters["Time"] as string);
                    splitter.SplitAtHour(userIds, inAt, outAt, hour, currentUser);
                    break;
            }

            return Ok();
        }
        
        // POST: odata/Punches/Default.PopulateRate
        [HttpPost]
        public IHttpActionResult PopulateRate(ODataActionParameters parameters)
        {
            var populateRateOptions = parameters["PopulateRateOptions"] as PopulateRateOptions;
            var inAt = populateRateOptions.InAt;
            var outAt = populateRateOptions.OutAt;
            var currentUser = CurrentUser();

            var punches = db.Punches
                .Where(p => p.InAt >= inAt && p.OutAt.HasValue && p.OutAt.Value <= outAt);
            var userIds = punches
                .GroupBy(p => p.UserId)
                .Select(g => g.Key);

            foreach (var option in populateRateOptions.Options)
            {
                switch (option.Type)
                {
                    case "count":

                        if (option.CountScope == "day")
                        {
                        }
                        else if (option.CountScope == "total")
                        {
                            // Loop the punches and populate the alternate rate
                            var filtered = punches.Where(p => p.UserId == 0).OrderBy(p => p.InAt);
                            var count = 0;
                            foreach (var punch in filtered)
                            {
                                count += punch.Minutes;

                                if (count > option.CountMinute)
                                {
                                    punch.PayrollRateId = option.AlternatePayrollRateId;
                                }
                            }
                        }

                        break;
                    case "range":

                        if (option.RangeDirection == "before")
                        {
                            // loop the punches and populate the alternate at each mark
                        }
                        else if (option.RangeDirection == "after")
                        {
                            // loop the punches and populate the alternate at each mark
                        }

                        break;
                }
            }

            return Created("");
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