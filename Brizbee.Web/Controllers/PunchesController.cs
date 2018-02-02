using Brizbee.Common.Models;
using Brizbee.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
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
        [EnableQuery(PageSize = 20, MaxExpansionDepth = 1)]
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