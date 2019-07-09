using Brizbee.Common.Exceptions;
using Brizbee.Common.Models;
using Brizbee.Web.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Http;
using static Brizbee.Web.Controllers.ReportsController;

namespace Brizbee.Web.Controllers
{
    public class ExportsController : ApiController
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();

        // GET: api/Exports/Csv
        [Route("api/Exports/Csv")]
        public IHttpActionResult GetCsv(
            [FromUri] string Delimiter,
            [FromUri] int? CommitId = null,
            [FromUri] DateTime? InAt = null,
            [FromUri] DateTime? OutAt = null)
        {
            if (CommitId.HasValue)
            {
                var commit = db.Commits.Find(CommitId.Value);
                var exportService = new ExportService(commit.Id);

                string csv = exportService.BuildCsv(Delimiter);
                var bytes = Encoding.UTF8.GetBytes(csv);
                return new FileActionResult(bytes, "text/plain",
                    string.Format(
                        "Committed Punches {0} thru {1}.csv",
                        commit.InAt.ToShortDateString(),
                        commit.OutAt.ToShortDateString()
                        ),
                    Request);
            }
            else if (InAt.HasValue && OutAt.HasValue)
            {
                var exportService = new ExportService(InAt.Value, OutAt.Value);

                string csv = exportService.BuildCsv(Delimiter);
                var bytes = Encoding.UTF8.GetBytes(csv);
                return new FileActionResult(bytes, "text/plain",
                    string.Format(
                        "Uncommitted Punches {0} thru {1}.csv",
                        InAt.Value.ToShortDateString(),
                        OutAt.Value.ToShortDateString()
                        ),
                    Request);
            }
            else
            {
                throw new NotAuthorizedException("Must specify a date range or commit id to export punches.");
            }
        }

        public User CurrentUser()
        {
            if (ActionContext.RequestContext.Principal.Identity.Name.Length > 0)
            {
                var currentUserId = int.Parse(ActionContext.RequestContext.Principal.Identity.Name);
                return db.Users
                    .Where(u => u.Id == currentUserId)
                    .FirstOrDefault();
            }
            else
            {
                return null;
            }
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
            }
            base.Dispose(disposing);
        }
    }
}