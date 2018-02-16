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
    public class OrganizationsController : BaseODataController
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();
        private OrganizationRepository repo = new OrganizationRepository();

        // PATCH: odata/Organizations(5)
        [AcceptVerbs("PATCH", "MERGE")]
        public IHttpActionResult Patch([FromODataUri] int key, Delta<Organization> patch)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var organization = repo.Update(key, patch, CurrentUser());

            return Updated(organization);
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