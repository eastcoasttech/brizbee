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
    public class RatesController : BaseODataController
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();
        private RateRepository repo = new RateRepository();

        // GET: odata/Rates
        [EnableQuery(PageSize = 20, MaxExpansionDepth = 1)]
        public IQueryable<Rate> GetRates()
        {
            return repo.GetAll(CurrentUser());
        }

        // GET: odata/Rates(5)
        [EnableQuery]
        public SingleResult<Rate> GetRate([FromODataUri] int key)
        {
            return SingleResult.Create(repo.Get(key, CurrentUser()));
        }

        // POST: odata/Rates
        public IHttpActionResult Post(Rate rate)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            rate = repo.Create(rate, CurrentUser());

            return Created(rate);
        }

        // PATCH: odata/Rates(5)
        [AcceptVerbs("PATCH", "MERGE")]
        public IHttpActionResult Patch([FromODataUri] int key, Delta<Rate> patch)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var rate = repo.Update(key, patch, CurrentUser());

            return Updated(rate);
        }

        // DELETE: odata/Rates(5)
        public IHttpActionResult Delete([FromODataUri] int key)
        {
            repo.Delete(key, CurrentUser());
            return StatusCode(HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Disposes of the resources used during each request (instance)
        /// of this controller.
        /// </summary>
        /// <param name="disposing">Whether or not the resources should be disposed</param>
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