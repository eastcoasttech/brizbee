using Brizbee.Common.Models;
using Brizbee.Web.Repositories;
using Brizbee.Web.Services;
using Microsoft.AspNet.OData;
using System;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;

namespace Brizbee.Web.Controllers
{
    public class PunchesController : BaseODataController
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();
        private PunchRepository repo = new PunchRepository();

        // GET: odata/Punches
        [EnableQuery(PageSize = 20, MaxExpansionDepth = 3)]
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

        // POST: odata/Punches/Default.PunchIn
        [HttpPost]
        public IHttpActionResult PunchIn(ODataActionParameters parameters)
        {
            var taskId = (int)parameters["TaskId"];
            var source = (string)parameters["SourceForInAt"];
            var timezone = (string)parameters["InAtTimeZone"];
            var latitudeForInAt = (string)parameters["LatitudeForInAt"];
            var longitudeForInAt = (string)parameters["LongitudeForInAt"];

            try
            {
                var punch = repo.PunchIn(taskId, CurrentUser(),
                    string.Format("{0} @ {1} @ {2}",
                        source,
                        HttpContext.Current.Request.UserHostAddress,
                        HttpContext.Current.Request.UserAgent),
                    timezone, latitudeForInAt, longitudeForInAt);
                return Created(punch);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning(ex.ToString());
                return Content(HttpStatusCode.NoContent, ex.ToString());
            }
        }

        // POST: odata/Punches/Default.PunchOut
        [HttpPost]
        public IHttpActionResult PunchOut(ODataActionParameters parameters)
        {
            var source = (string)parameters["SourceForOutAt"];
            var timezone = (string)parameters["OutAtTimeZone"];

            try
            {
                var punch = repo.PunchOut(CurrentUser(),
                    string.Format("{0} @ {1} @ {2}",
                        source,
                        HttpContext.Current.Request.UserHostAddress,
                        HttpContext.Current.Request.UserAgent),
                    timezone);
                return Created(punch);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning(ex.ToString());
                return Content(HttpStatusCode.NoContent, ex.ToString());
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