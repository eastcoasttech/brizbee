using Brizbee.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace Brizbee.Web.Controllers
{
    public class QuickBooksDesktopController : ApiController
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();

        // POST: api/QuickBooksDesktop/SaveExportDetails
        [HttpPost]
        [Route("api/QuickBooksDesktop/SaveExportDetails")]
        public IHttpActionResult PostSaveExportDetails([FromBody]QuickBooksDesktopExport quickBooksDesktopExport)
        {
            db.QuickBooksDesktopExports.Add(quickBooksDesktopExport);
            db.SaveChanges();

            return Created("api/QuickBooksDesktop/SaveExportDetails", "");
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