using Brizbee.Common.Models;
using Brizbee.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.OData;

namespace Brizbee.Controllers
{
    public class JobsController : BaseODataController
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();
        private JobRepository repo = new JobRepository();

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