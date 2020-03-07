using Brizbee.Common.Database;
using Brizbee.Common.Models;
using Brizbee.Web.Repositories;
using Microsoft.AspNet.OData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;

namespace Brizbee.Web.Controllers
{
    public class TimesheetEntriesController : BaseODataController
    {
        private SqlContext db = new SqlContext();
        private TimesheetEntryRepository repo = new TimesheetEntryRepository();

        // GET: odata/TimesheetEntries
        [EnableQuery(PageSize = 20, MaxExpansionDepth = 3)]
        public IQueryable<TimesheetEntry> GetTimesheetEntries()
        {
            return repo.GetAll(CurrentUser());
        }

        // GET: odata/TimesheetEntries(5)
        [EnableQuery]
        public SingleResult<TimesheetEntry> GetTimesheetEntry([FromODataUri] int key)
        {
            return SingleResult.Create(repo.Get(key, CurrentUser()));
        }

        // POST: odata/TimesheetEntries
        public IHttpActionResult Post(TimesheetEntry timesheetEntry)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            timesheetEntry = repo.Create(timesheetEntry, CurrentUser());

            return Created(timesheetEntry);
        }

        // PATCH: odata/TimesheetEntries(5)
        [AcceptVerbs("PATCH", "MERGE")]
        public IHttpActionResult Patch([FromODataUri] int key, Delta<TimesheetEntry> patch)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var timesheetEntry = repo.Update(key, patch, CurrentUser());

            return Updated(timesheetEntry);
        }

        // DELETE: odata/TimesheetEntries(5)
        public IHttpActionResult Delete([FromODataUri] int key)
        {
            repo.Delete(key, CurrentUser());
            return StatusCode(HttpStatusCode.NoContent);
        }
    }
}