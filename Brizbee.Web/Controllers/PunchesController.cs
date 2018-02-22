using Brizbee.Common.Models;
using Brizbee.Repositories;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using System.Web.OData;

namespace Brizbee.Controllers
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
            var queryable = new List<Punch>() { repo.Get(key, CurrentUser()) }.AsQueryable();
            return SingleResult.Create(queryable);
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
        [EnableQuery(MaxExpansionDepth =3)]
        public SingleResult<Punch> Current()
        {
            var userId = CurrentUser().Id;
            var punch = db.Punches
                .Where(p => p.UserId == userId)
                .Where(p => p.OutAt == null)
                .OrderByDescending(p => p.InAt)
                .Take(1);

            try
            {
                return SingleResult.Create(punch);
            }
            catch (Exception)
            {
                return SingleResult.Create(Enumerable.Empty<Punch>().AsQueryable());
            }
        }

        // POST: odata/Punches/Default.PunchIn
        [HttpPost]
        public IHttpActionResult PunchIn(ODataActionParameters parameters)
        {
            var taskId = (int)parameters["TaskId"];

            try
            {
                repo.PunchIn(taskId, CurrentUser());
                return Ok();
            }
            catch (Exception ex)
            {
                Trace.TraceWarning(ex.ToString());
                return BadRequest();
            }
        }

        // POST: odata/Punches/Default.PunchOut
        [HttpPost]
        public IHttpActionResult PunchOut(ODataActionParameters parameters)
        {
            try
            {
                repo.PunchOut(CurrentUser());
                return Ok();
            }
            catch (Exception ex)
            {
                Trace.TraceWarning(ex.ToString());
                return BadRequest();
            }
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