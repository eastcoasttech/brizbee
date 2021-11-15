//
//  ReportsController.cs
//  BRIZBEE API
//
//  Copyright (C) 2019-2021 East Coast Technology Services, LLC
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

using Brizbee.Api.Services;
using Brizbee.Api.Services.Reports;
using Brizbee.Core.Models;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;

namespace Brizbee.Api.Controllers
{
    public class ReportsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly SqlContext _context;
        private readonly TelemetryClient _telemetryClient;

        public ReportsController(IConfiguration configuration, SqlContext context, TelemetryClient telemetryClient)
        {
            _configuration = configuration;
            _context = context;
            _telemetryClient = telemetryClient;
        }

        // GET: api/Reports/PunchesByUser
        [HttpGet("api/Reports/PunchesByUser")]
        public IActionResult GetPunchesByUser(
            [FromQuery] string UserScope,
            [FromQuery] int[] UserIds,
            [FromQuery] string JobScope,
            [FromQuery] int[] JobIds,
            [FromQuery] DateTime Min,
            [FromQuery] DateTime Max,
            [FromQuery] string CommitStatus)
        {
            var currentUser = CurrentUser();

            _telemetryClient.TrackTrace($"Generating PunchesByUser report for {Min:G} through {Max:G}");

            // Ensure that user is authorized.
            if (!currentUser.CanViewReports)
                return Forbid();

            var bytes = new PunchesByUserAsExcel().Build(_context, currentUser, Min, Max, UserScope, UserIds, JobScope, JobIds, CommitStatus);
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileDownloadName: $"Report - Punches by User {Min.ToString("yyyy_MM_dd")} thru {Max.ToString("yyyy_MM_dd")}.xlsx");
        }

        // GET: api/Reports/PunchesByJob
        [HttpGet("api/Reports/PunchesByJob")]
        public IActionResult GetPunchesByJob(
            [FromQuery] string UserScope,
            [FromQuery] int[] UserIds,
            [FromQuery] string JobScope,
            [FromQuery] int[] JobIds,
            [FromQuery] DateTime Min,
            [FromQuery] DateTime Max,
            [FromQuery] string CommitStatus)
        {
            var currentUser = CurrentUser();

            _telemetryClient.TrackTrace($"Generating PunchesByJob report for {Min:G} through {Max:G}");

            // Ensure that user is authorized.
            if (!currentUser.CanViewReports)
                return Forbid();

            var bytes = new PunchesByProjectAsExcel().Build(_context, currentUser, Min, Max, UserScope, UserIds, JobScope, JobIds, CommitStatus);
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileDownloadName: $"Report - Punches by Project {Min.ToString("yyyy_MM_dd")} thru {Max.ToString("yyyy_MM_dd")}.xlsx");
        }

        // GET: api/Reports/PunchesByDay
        [HttpGet("api/Reports/PunchesByDay")]
        public IActionResult GetPunchesByDay(
            [FromQuery] string UserScope,
            [FromQuery] int[] UserIds,
            [FromQuery] string JobScope,
            [FromQuery] int[] JobIds,
            [FromQuery] DateTime Min,
            [FromQuery] DateTime Max,
            [FromQuery] string CommitStatus)
        {
            var currentUser = CurrentUser();

            _telemetryClient.TrackTrace($"Generating PunchesByDay report for {Min:G} through {Max:G}");

            // Ensure that user is authorized.
            if (!currentUser.CanViewReports)
                return Forbid();

            var bytes = new PunchesByDayAsExcel().Build(_context, currentUser, Min, Max, UserScope, UserIds, JobScope, JobIds, CommitStatus);
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileDownloadName: $"Report - Punches by Day {Min.ToString("yyyy_MM_dd")} thru {Max.ToString("yyyy_MM_dd")}.xlsx");
        }

        // GET: api/Reports/TimeEntriesByUser
        [HttpGet("api/Reports/TimeEntriesByUser")]
        public IActionResult GetTimeEntriesByUser(
            [FromQuery] string UserScope,
            [FromQuery] int[] UserIds,
            [FromQuery] string JobScope,
            [FromQuery] int[] JobIds,
            [FromQuery] DateTime Min,
            [FromQuery] DateTime Max)
        {
            var currentUser = CurrentUser();

            _telemetryClient.TrackTrace($"Generating TimeEntriesByUser report for {Min:G} through {Max:G}");

            // Ensure that user is authorized.
            if (!currentUser.CanViewReports)
                return Forbid();

            var bytes = new TimeCardsByUserAsExcel().Build(_context, currentUser, Min, Max, UserScope, UserIds, JobScope, JobIds);
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileDownloadName: $"Report - Time Cards by User {Min.ToString("yyyy_MM_dd")} thru {Max.ToString("yyyy_MM_dd")}.xlsx");
        }

        // GET: api/Reports/TimeEntriesByJob
        [HttpGet("api/Reports/TimeEntriesByJob")]
        public IActionResult GetTimeEntriesByJob(
            [FromQuery] string UserScope,
            [FromQuery] int[] UserIds,
            [FromQuery] string JobScope,
            [FromQuery] int[] JobIds,
            [FromQuery] DateTime Min,
            [FromQuery] DateTime Max)
        {
            var currentUser = CurrentUser();

            _telemetryClient.TrackTrace($"Generating TimeEntriesByJob report for {Min:G} through {Max:G}");

            // Ensure that user is authorized.
            if (!currentUser.CanViewReports)
                return Forbid();

            var bytes = new TimeCardsByProjectAsExcel().Build(_context, currentUser, Min, Max, UserScope, UserIds, JobScope, JobIds);
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileDownloadName: $"Report - Time Cards by Project {Min.ToString("yyyy_MM_dd")} thru {Max.ToString("yyyy_MM_dd")}.xlsx");
        }

        // GET: api/Reports/TimeEntriesByDay
        [HttpGet("api/Reports/TimeEntriesByDay")]
        public IActionResult GetTimeEntriesByDay(
            [FromQuery] string UserScope,
            [FromQuery] int[] UserIds,
            [FromQuery] string JobScope,
            [FromQuery] int[] JobIds,
            [FromQuery] DateTime Min,
            [FromQuery] DateTime Max)
        {
            var currentUser = CurrentUser();

            _telemetryClient.TrackTrace($"Generating TimeEntriesByDay report for {Min:G} through {Max:G}");

            // Ensure that user is authorized.
            if (!currentUser.CanViewReports)
                return Forbid();

            var bytes = new TimeCardsByDayAsExcel().Build(_context, currentUser, Min, Max, UserScope, UserIds, JobScope, JobIds);
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileDownloadName: $"Report - Time Cards by Day {Min.ToString("yyyy_MM_dd")} thru {Max.ToString("yyyy_MM_dd")}.xlsx");
        }

        // GET: api/Reports/TasksByJob
        [HttpGet("api/Reports/TasksByJob")]
        public IActionResult GetTasksByJob([FromQuery] int JobId, [FromQuery] string taskGroupScope = "")
        {
            var currentUser = CurrentUser();

            _telemetryClient.TrackTrace($"Generating TasksByJob report for project {JobId}");

            var project = _context.Jobs
                .Where(p => p.Id == JobId)
                .FirstOrDefault();

            var bytes = new ReportBuilder().TasksByProjectAsPdf(_context, JobId, CurrentUser(), taskGroupScope);
            return File(
                bytes,
                "application/pdf",
                fileDownloadName: string.Format("Tasks by Project for {0} - {1}.pdf", project.Number, project.Name));
        }

        private User CurrentUser()
        {
            var type = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
            var sub = HttpContext.User.Claims.FirstOrDefault(c => c.Type == type).Value;
            var currentUserId = int.Parse(sub);
            return _context.Users
                .Where(u => u.Id == currentUserId)
                .FirstOrDefault();
        }
    }
}