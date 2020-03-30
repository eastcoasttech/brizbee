using Brizbee.Common.Database;
using Brizbee.Common.Models;
using Brizbee.Web.Services;
using NodaTime;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Web.Http;

namespace Brizbee.Web.Controllers
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
                response.Content.Headers.ContentLength = this.bytes.Length;

                return System.Threading.Tasks.Task.FromResult(response);
            }
        }

        private SqlContext db = new SqlContext();

        // GET: api/Reports/PunchesByUser
        [Route("api/Reports/PunchesByUser")]
        public IHttpActionResult GetPunchesByUser(
            [FromUri] string UserScope,
            [FromUri] int[] UserIds,
            [FromUri] string JobScope,
            [FromUri] int[] JobIds,
            [FromUri] DateTime Min,
            [FromUri] DateTime Max,
            [FromUri] string CommitStatus)
        {
            var currentUser = CurrentUser();
            var bytes = new ReportBuilder().PunchesByUserAsPdf(UserScope, UserIds, JobScope, JobIds, Min, Max, CommitStatus, currentUser);
            return new FileActionResult(bytes, "application/pdf",
                string.Format(
                    "Punches by User {0} thru {1}.pdf",
                    Min.ToShortDateString(),
                    Max.ToShortDateString()),
                Request);
        }

        // GET: api/Reports/PunchesByJob
        [Route("api/Reports/PunchesByJob")]
        public IHttpActionResult GetPunchesByJob(
            [FromUri] string UserScope,
            [FromUri] int[] UserIds,
            [FromUri] string JobScope,
            [FromUri] int[] JobIds,
            [FromUri] DateTime Min,
            [FromUri] DateTime Max,
            [FromUri] string CommitStatus)
        {
            var currentUser = CurrentUser();
            var bytes = new ReportBuilder().PunchesByJobAndTaskAsPdf(UserScope, UserIds, JobScope, JobIds, Min, Max, CommitStatus, currentUser);
            return new FileActionResult(bytes, "application/pdf",
                string.Format(
                    "Punches by Job and Task {0} thru {1}.pdf",
                    Min.ToShortDateString(),
                    Max.ToShortDateString()),
                Request);
        }

        // GET: api/Reports/PunchesByDay
        [Route("api/Reports/PunchesByDay")]
        public IHttpActionResult GetPunchesByDay(
            [FromUri] string UserScope,
            [FromUri] int[] UserIds,
            [FromUri] string JobScope,
            [FromUri] int[] JobIds,
            [FromUri] DateTime Min,
            [FromUri] DateTime Max,
            [FromUri] string CommitStatus)
        {
            var currentUser = CurrentUser();
            var bytes = new ReportBuilder().PunchesByDayAsPdf(UserScope, UserIds, JobScope, JobIds, Min, Max, CommitStatus, currentUser);
            return new FileActionResult(bytes, "application/pdf",
                string.Format(
                    "Punches by Day {0} thru {1}.pdf",
                    Min.ToShortDateString(),
                    Max.ToShortDateString()),
                Request);
        }

        // GET: api/Reports/TimeEntriesByUser
        [Route("api/Reports/TimeEntriesByUser")]
        public IHttpActionResult GetTimeEntriesByUser(
            [FromUri] string UserScope,
            [FromUri] int[] UserIds,
            [FromUri] string JobScope,
            [FromUri] int[] JobIds,
            [FromUri] DateTime Min,
            [FromUri] DateTime Max)
        {
            var currentUser = CurrentUser();
            var bytes = new ReportBuilder().TimeEntriesByUserAsPdf(UserScope, UserIds, JobScope, JobIds, Min, Max, currentUser);
            return new FileActionResult(bytes, "application/pdf",
                string.Format(
                    "Time Entries by User {0} thru {1}.pdf",
                    Min.ToShortDateString(),
                    Max.ToShortDateString()),
                Request);
        }

        // GET: api/Reports/TimeEntriesByJob
        [Route("api/Reports/TimeEntriesByJob")]
        public IHttpActionResult GetTimeEntriesByJob(
            [FromUri] string UserScope,
            [FromUri] int[] UserIds,
            [FromUri] string JobScope,
            [FromUri] int[] JobIds,
            [FromUri] DateTime Min,
            [FromUri] DateTime Max)
        {
            try
            {
                var currentUser = CurrentUser();
                var bytes = new ReportBuilder().TimeEntriesByJobAndTaskAsPdf(UserScope, UserIds, JobScope, JobIds, Min, Max, currentUser);
                return new FileActionResult(bytes, "application/pdf",
                    string.Format(
                        "Time Entries by Job and Task {0} thru {1}.pdf",
                        Min.ToShortDateString(),
                        Max.ToShortDateString()),
                    Request);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
        }

        // GET: api/Reports/TimeEntriesByDay
        [Route("api/Reports/TimeEntriesByDay")]
        public IHttpActionResult GetTimeEntriesByDay(
            [FromUri] string UserScope,
            [FromUri] int[] UserIds,
            [FromUri] string JobScope,
            [FromUri] int[] JobIds,
            [FromUri] DateTime Min,
            [FromUri] DateTime Max)
        {
            var currentUser = CurrentUser();
            var bytes = new ReportBuilder().TimeEntriesByDayAsPdf(UserScope, UserIds, JobScope, JobIds, Min, Max, currentUser);
            return new FileActionResult(bytes, "application/pdf",
                string.Format(
                    "Time Entries by Day {0} thru {1}.pdf",
                    Min.ToShortDateString(),
                    Max.ToShortDateString()),
                Request);
        }

        // GET: api/Reports/TasksByJob
        [Route("api/Reports/TasksByJob")]
        public IHttpActionResult GetTasksByJob([FromUri] int JobId)
        {
            var currentUser = CurrentUser();
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