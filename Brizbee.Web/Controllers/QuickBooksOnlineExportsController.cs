using Brizbee.Common.Database;
using Brizbee.Common.Models;
using Microsoft.AspNet.OData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace Brizbee.Web.Controllers
{
    public class QuickBooksOnlineExportsController : BaseODataController
    {
        private SqlContext db = new SqlContext();

        // GET: odata/QuickBooksOnlineExports
        [EnableQuery(PageSize = 20)]
        public IQueryable<QuickBooksOnlineExport> GetQuickBooksOnlineExports()
        {
            var currentUser = CurrentUser();
            var commitIds = db.Commits
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Select(c => c.Id);
            return db.QuickBooksOnlineExports
                .Where(q => commitIds.Contains(q.CommitId.Value));
        }

        // GET: odata/QuickBooksOnlineExports(5)
        [EnableQuery]
        public SingleResult<QuickBooksOnlineExport> GetQuickBooksOnlineExport([FromODataUri] int key)
        {
            var currentUser = CurrentUser();
            var commitIds = db.Commits
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Select(c => c.Id);
            var quickBooksOnlineExport = db.QuickBooksOnlineExports
                .Where(q => commitIds.Contains(q.CommitId.Value))
                .Where(q => q.Id == key);

            return SingleResult.Create(quickBooksOnlineExport);
        }

        // POST: odata/QuickBooksOnlineExports
        public IHttpActionResult Post(QuickBooksOnlineExport quickBooksOnlineExport)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.QuickBooksOnlineExports.Add(quickBooksOnlineExport);
            db.SaveChanges();

            return Created(quickBooksOnlineExport);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}