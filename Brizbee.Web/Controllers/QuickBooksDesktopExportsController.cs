using Brizbee.Common.Models;
using Microsoft.AspNet.OData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace Brizbee.Web.Controllers
{
    public class QuickBooksDesktopExportsController : BaseODataController
    {
        private SqlContext db = new SqlContext();

        // GET: odata/QuickBooksDesktopExports
        [EnableQuery(PageSize = 20)]
        public IQueryable<QuickBooksDesktopExport> GetQuickBooksDesktopExports()
        {
            var currentUser = CurrentUser();
            var commitIds = db.Commits
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Select(c => c.Id);
            return db.QuickBooksDesktopExports
                .Where(q => commitIds.Contains(q.CommitId.Value));
        }

        // GET: odata/QuickBooksDesktopExports(5)
        [EnableQuery]
        public SingleResult<QuickBooksDesktopExport> GetQuickBooksDesktopExport([FromODataUri] int key)
        {
            var currentUser = CurrentUser();
            var commitIds = db.Commits
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Select(c => c.Id);
            var quickBooksDesktopExport = db.QuickBooksDesktopExports
                .Where(q => commitIds.Contains(q.CommitId.Value))
                .Where(q => q.Id == key);

            return SingleResult.Create(quickBooksDesktopExport);
        }

        // POST: odata/QuickBooksDesktopExports
        public IHttpActionResult Post(QuickBooksDesktopExport quickBooksDesktopExport)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.QuickBooksDesktopExports.Add(quickBooksDesktopExport);
            db.SaveChanges();

            return Created(quickBooksDesktopExport);
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