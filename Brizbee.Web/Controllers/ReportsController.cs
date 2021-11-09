//
//  ReportsController.cs
//  BRIZBEE API
//
//  Copyright (C) 2020 East Coast Technology Services, LLC
//
//  This file is part of the BRIZBEE API.
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Affero General Public License as
//  published by the Free Software Foundation, either version 3 of the
//  License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Affero General Public License for more details.
//
//  You should have received a copy of the GNU Affero General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
//

using Brizbee.Common.Models;
using Brizbee.Web.Services;
using Brizbee.Web.Services.Reports;
using Microsoft.ApplicationInsights;
using System;
using System.Diagnostics;
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
        private TelemetryClient telemetryClient = new TelemetryClient();

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

            telemetryClient.TrackTrace($"Generating PunchesByUser report for {Min:G} through {Max:G}");

            // Ensure that user is authorized.
            if (!currentUser.CanViewReports)
                return StatusCode(HttpStatusCode.Forbidden);

            var bytes = new PunchesByUserAsExcel().Build(currentUser, Min, Max, UserScope, UserIds, JobScope, JobIds, CommitStatus);
            return new FileActionResult(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Report - Punches by User {Min.ToString("yyyy_MM_dd")} thru {Max.ToString("yyyy_MM_dd")}.xlsx",
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

            telemetryClient.TrackTrace($"Generating PunchesByJob report for {Min:G} through {Max:G}");

            // Ensure that user is authorized.
            if (!currentUser.CanViewReports)
                return StatusCode(HttpStatusCode.Forbidden);

            var bytes = new PunchesByProjectAsExcel().Build(currentUser, Min, Max, UserScope, UserIds, JobScope, JobIds, CommitStatus);
            return new FileActionResult(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Report - Punches by Project {Min.ToString("yyyy_MM_dd")} thru {Max.ToString("yyyy_MM_dd")}.xlsx",
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

            telemetryClient.TrackTrace($"Generating PunchesByDay report for {Min:G} through {Max:G}");

            // Ensure that user is authorized.
            if (!currentUser.CanViewReports)
                return StatusCode(HttpStatusCode.Forbidden);

            var bytes = new PunchesByDayAsExcel().Build(currentUser, Min, Max, UserScope, UserIds, JobScope, JobIds, CommitStatus);
            return new FileActionResult(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Report - Punches by Day {Min.ToString("yyyy_MM_dd")} thru {Max.ToString("yyyy_MM_dd")}.xlsx",
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

            telemetryClient.TrackTrace($"Generating TimeEntriesByUser report for {Min:G} through {Max:G}");

            // Ensure that user is authorized.
            if (!currentUser.CanViewReports)
                return StatusCode(HttpStatusCode.Forbidden);

            var bytes = new TimeCardsByUserAsExcel().Build(currentUser, Min, Max, UserScope, UserIds, JobScope, JobIds);
            return new FileActionResult(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Report - Time Cards by User {Min.ToString("yyyy_MM_dd")} thru {Max.ToString("yyyy_MM_dd")}.xlsx",
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
            var currentUser = CurrentUser();

            telemetryClient.TrackTrace($"Generating TimeEntriesByJob report for {Min:G} through {Max:G}");

            // Ensure that user is authorized.
            if (!currentUser.CanViewReports)
                return StatusCode(HttpStatusCode.Forbidden);

            var bytes = new TimeCardsByProjectAsExcel().Build(currentUser, Min, Max, UserScope, UserIds, JobScope, JobIds);
            return new FileActionResult(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Report - Time Cards by Project {Min.ToString("yyyy_MM_dd")} thru {Max.ToString("yyyy_MM_dd")}.xlsx",
                Request);
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

            telemetryClient.TrackTrace($"Generating TimeEntriesByDay report for {Min:G} through {Max:G}");

            // Ensure that user is authorized.
            if (!currentUser.CanViewReports)
                return StatusCode(HttpStatusCode.Forbidden);

            var bytes = new TimeCardsByDayAsExcel().Build(currentUser, Min, Max, UserScope, UserIds, JobScope, JobIds);
            return new FileActionResult(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Report - Time Cards by Day {Min.ToString("yyyy_MM_dd")} thru {Max.ToString("yyyy_MM_dd")}.xlsx",
                Request);
        }

        // GET: api/Reports/TasksByJob
        [Route("api/Reports/TasksByJob")]
        public IHttpActionResult GetTasksByJob([FromUri] int JobId, [FromUri] string taskGroupScope = "")
        {
            var currentUser = CurrentUser();

            telemetryClient.TrackTrace($"Generating TasksByJob report for project {JobId}");

            var project = db.Jobs
                .Where(p => p.Id == JobId)
                .FirstOrDefault();

            var bytes = new ReportBuilder().TasksByProjectAsPdf(JobId, CurrentUser(), taskGroupScope);
            return new FileActionResult(bytes, "application/pdf",
                string.Format(
                    "Tasks by Project for {0} - {1}.pdf",
                    project.Number,
                    project.Name),
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();

                telemetryClient.Flush();
            }
            base.Dispose(disposing);
        }
    }
}