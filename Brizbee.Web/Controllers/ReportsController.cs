using Brizbee.Common.Models;
using Brizbee.Services;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace Brizbee.Controllers
{
    public class ReportsController : ApiController
    {
        public class FileActionResult : IHttpActionResult
        {
            private byte[] bytes;
            private string contentType;
            private string fileName;
            private HttpRequestMessage request;

            public FileActionResult(byte[] bytes, string contentType, string fileName, HttpRequestMessage request)
            {
                //if (filePath == null) throw new ArgumentNullException("filePath");

                this.bytes = bytes;
                this.contentType = contentType;
                this.fileName = fileName;
                this.request = request;
            }
            
            public System.Threading.Tasks.Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(this.bytes)
                };

                //var contentType = this.contentType ?? MimeMapping.GetMimeMapping(Path.GetExtension(this.filePath));
                response.Content.Headers.ContentType = new MediaTypeHeaderValue(this.contentType);
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = this.fileName
                };

                return System.Threading.Tasks.Task.FromResult(response);
            }
        }

        private BrizbeeWebContext db = new BrizbeeWebContext();

        // GET: api/Reports/PunchesByUser
        [Route("api/Reports/PunchesByUser")]
        public IHttpActionResult GetPunchesByUser([FromUri] int[] UserIds, [FromUri] DateTime Min, [FromUri] DateTime Max)
        {
            var organization = db.Organizations.Find(CurrentUser().OrganizationId);
            var tz = TimeZoneInfo.FindSystemTimeZoneById(organization.TimeZone);
            var bytes = new ReportBuilder().PunchesByUserAsPdf(UserIds, Min, Max, CurrentUser());
            return new FileActionResult(bytes, "application/pdf",
                string.Format(
                    "Punches by User {0} thru {1}.pdf",
                    TimeZoneInfo.ConvertTime(Min, tz).ToShortDateString(),
                    TimeZoneInfo.ConvertTime(Max, tz).ToShortDateString()),
                Request);
        }

        // GET: api/Reports/PunchesByJob
        [Route("api/Reports/PunchesByJob")]
        public IHttpActionResult GetPunchesByJob([FromUri] int[] UserIds, [FromUri] int[] JobIds, [FromUri] DateTime Min, [FromUri] DateTime Max)
        {
            var organization = db.Organizations.Find(CurrentUser().OrganizationId);
            var tz = TimeZoneInfo.FindSystemTimeZoneById(organization.TimeZone);
            var bytes = new ReportBuilder().PunchesByJobAndTaskAsPdf(UserIds, JobIds, Min, Max, CurrentUser());
            return new FileActionResult(bytes, "application/pdf",
                string.Format(
                    "Punches by Job and Task {0} thru {1}.pdf",
                    TimeZoneInfo.ConvertTime(Min, tz).ToShortDateString(),
                    TimeZoneInfo.ConvertTime(Max, tz).ToShortDateString()),
                Request);
        }

        // GET: api/Reports/PunchesByDay
        [Route("api/Reports/PunchesByDay")]
        public IHttpActionResult GetPunchesByDay([FromUri] int[] UserIds, [FromUri] int[] JobIds, [FromUri] DateTime Min, [FromUri] DateTime Max)
        {
            var organization = db.Organizations.Find(CurrentUser().OrganizationId);
            var tz = TimeZoneInfo.FindSystemTimeZoneById(organization.TimeZone);
            var bytes = new ReportBuilder().PunchesByDayAsPdf(UserIds, JobIds, Min, Max, CurrentUser());
            return new FileActionResult(bytes, "application/pdf",
                string.Format(
                    "Punches by Day {0} thru {1}.pdf",
                    TimeZoneInfo.ConvertTime(Min, tz).ToShortDateString(),
                    TimeZoneInfo.ConvertTime(Max, tz).ToShortDateString()),
                Request);
        }

        // GET: api/Reports/TasksByJob
        [Route("api/Reports/TasksByJob")]
        public IHttpActionResult GetTasksByJob([FromUri] int JobId)
        {
            var organization = db.Organizations.Find(CurrentUser().OrganizationId);
            var tz = TimeZoneInfo.FindSystemTimeZoneById(organization.TimeZone);
            var job = db.Jobs.Where(j => j.Id == JobId).FirstOrDefault();
            var bytes = new ReportBuilder().TasksByJobAsPdf(JobId, CurrentUser());
            return new FileActionResult(bytes, "application/pdf",
                string.Format(
                    "Tasks by Job for {0} - {1}.pdf",
                    job.Number,
                    job.Name),
                Request);
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
    }
}