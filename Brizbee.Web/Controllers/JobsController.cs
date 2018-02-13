using Brizbee.Common.Models;
using Brizbee.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.OData;

namespace Brizbee.Controllers
{
    public class JobsController : BaseODataController
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();
        private JobRepository repo = new JobRepository();

        // GET: odata/Jobs
        [EnableQuery(PageSize = 20, MaxExpansionDepth = 1)]
        public IQueryable<Job> GetJobs()
        {
            try
            {
                return repo.GetAll(CurrentUser());
            }
            catch (Exception)
            {
                // Return an empty result if there are errors
                return Enumerable.Empty<Job>().AsQueryable();
            }
        }

        // GET: odata/Jobs(5)
        [EnableQuery]
        public SingleResult<Job> GetJob([FromODataUri] int key)
        {
            try
            {
                var queryable = new List<Job>() { repo.Get(key, CurrentUser()) }.AsQueryable();
                return SingleResult.Create(queryable);
            }
            catch (Exception)
            {
                // Return an empty result if there are errors
                return SingleResult.Create(Enumerable.Empty<Job>().AsQueryable());
            }
        }

        // POST: odata/Jobs
        public IHttpActionResult Post(Job job)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            job = repo.Create(job, CurrentUser());

            return Created(job);
        }

        // DELETE: odata/Jobs(5)
        public IHttpActionResult Delete([FromODataUri] int key)
        {
            repo.Delete(key, CurrentUser());
            return StatusCode(HttpStatusCode.NoContent);
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