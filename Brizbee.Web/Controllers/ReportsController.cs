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
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using NodaTime;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Web.Http;
using System.IO;
using System.Text;
using System.Data.Entity.SqlServer;
using System.Data.Entity;

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

            Trace.TraceInformation($"Generating PunchesByJob report for {Min:G} through {Max:G}");


            // Ensure that user is authorized.
            if (!currentUser.CanViewReports)
                return StatusCode(HttpStatusCode.Forbidden);

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

            Trace.TraceInformation($"Generating PunchesByDay report for {Min:G} through {Max:G}");


            // Ensure that user is authorized.
            if (!currentUser.CanViewReports)
                return StatusCode(HttpStatusCode.Forbidden);

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

            // Ensure that user is authorized.
            if (!currentUser.CanViewReports)
                return StatusCode(HttpStatusCode.Forbidden);


            Trace.TraceInformation($"Generating TimeEntriesByUser report for {Min:G} through {Max:G}");

            var bytes = new ReportBuilder().TimeEntriesByUserAsPdf(UserScope, UserIds, JobScope, JobIds, Min, Max, currentUser);
            return new FileActionResult(bytes, "application/pdf",
                string.Format(
                    "Time Cards by User {0} thru {1}.pdf",
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

                Trace.TraceInformation($"Generating TimeEntriesByJob report for {Min:G} through {Max:G}");


                // Ensure that user is authorized.
                if (!currentUser.CanViewReports)
                    return StatusCode(HttpStatusCode.Forbidden);

                var bytes = new ReportBuilder().TimeEntriesByJobAndTaskAsPdf(UserScope, UserIds, JobScope, JobIds, Min, Max, currentUser);
                return new FileActionResult(bytes, "application/pdf",
                    string.Format(
                        "Time Cards by Job and Task {0} thru {1}.pdf",
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

            Trace.TraceInformation($"Generating TimeEntriesByDay report for {Min:G} through {Max:G}");


            // Ensure that user is authorized.
            if (!currentUser.CanViewReports)
                return StatusCode(HttpStatusCode.Forbidden);

            var bytes = new ReportBuilder().TimeEntriesByDayAsPdf(UserScope, UserIds, JobScope, JobIds, Min, Max, currentUser);
            return new FileActionResult(bytes, "application/pdf",
                string.Format(
                    "Time Cards by Day {0} thru {1}.pdf",
                    Min.ToShortDateString(),
                    Max.ToShortDateString()),
                Request);
        }

        // GET: api/Reports/TasksByJob
        [Route("api/Reports/TasksByJob")]
        public IHttpActionResult GetTasksByJob([FromUri] int JobId, [FromUri] string taskGroupScope = "")
        {
            var currentUser = CurrentUser();

            Trace.TraceInformation($"Generating TasksByJob report for project {JobId}");

            var job = db.Jobs.Where(j => j.Id == JobId).FirstOrDefault();
            var bytes = new ReportBuilder().TasksByJobAsPdf(JobId, CurrentUser(), taskGroupScope);
            return new FileActionResult(bytes, "application/pdf",
                string.Format(
                    "Tasks by Job for {0} - {1}.pdf",
                    job.Number,
                    job.Name),
                Request);
        }

        // GET: api/Reports/PunchesForPayroll
        [Route("api/Reports/PunchesForPayroll")]
        public IHttpActionResult GetPunchesForPayroll([FromUri] DateTime min, [FromUri] DateTime max)
        {
            var currentUser = CurrentUser();
            var organization = db.Organizations.Find(currentUser.OrganizationId);

            var reportTitle = $"PUNCHES FOR PAYROLL {min.ToString("M/d/yyyy")} thru {max.ToString("M/d/yyyy")} GENERATED {DateTime.Now.ToString("ddd, MMM d, yyyy h:mm:ss tt").ToUpperInvariant()}";
            var organizationName = organization.Name;

            // Ensure that user is authorized.
            if (!currentUser.CanViewReports)
                return StatusCode(HttpStatusCode.Forbidden);

            var styleSheet = new Stylesheet(
                new Fonts(

                    // Index 0 - Default font
                    new Font(
                        new FontSize() { Val = 9 },
                        new Color() { Rgb = new HexBinaryValue() { Value = "00000000" } },
                        new FontName() { Val = "Arial" }),

                    // Index 1 - Bold font
                    new Font(
                        new Bold(),
                        new FontSize() { Val = 9 },
                        new Color() { Rgb = new HexBinaryValue() { Value = "00000000" } },
                        new FontName() { Val = "Arial" }),

                    // Index 2 - Italic font
                    new Font(
                        new Italic(),
                        new FontSize() { Val = 9 },
                        new Color() { Rgb = new HexBinaryValue() { Value = "00000000" } },
                        new FontName() { Val = "Arial" }),

                    // Index 3 - White Bold Font
                    new Font(
                        new Bold(),
                        new FontSize() { Val = 9 },
                        new Color() { Rgb = new HexBinaryValue() { Value = "FFFFFFFF" } },
                        new FontName() { Val = "Arial" })
                ),
                new Fills(

                    // Index 0 - Dfault fill
                    new Fill(
                        new PatternFill() { PatternType = PatternValues.None }),

                    // Index 1 - The default fill of gray 125 (required)
                    new Fill(
                        new PatternFill() { PatternType = PatternValues.Gray125 }),

                    // Index 2 - The yellow fill
                    new Fill(
                        new PatternFill(
                            new ForegroundColor() { Rgb = new HexBinaryValue() { Value = "FFFFFF00" } }
                        )
                        { PatternType = PatternValues.Solid }),

                    // Index 3
                    new Fill(
                        new PatternFill(
                            new ForegroundColor() { Rgb = new HexBinaryValue() { Value = "00000000" } }
                        )
                        { PatternType = PatternValues.Solid })
                ),
                new Borders(

                    // Index 0 - Default border
                    new Border(
                        new LeftBorder(),
                        new RightBorder(),
                        new TopBorder(),
                        new BottomBorder(),
                        new DiagonalBorder()),

                    // Index 1 - Applies a Left, Right, Top, Bottom border to a cell
                    new Border(
                        new LeftBorder(
                            new Color() { Auto = true }
                        )
                        { Style = BorderStyleValues.Thin },
                        new RightBorder(
                            new Color() { Auto = true }
                        )
                        { Style = BorderStyleValues.Thin },
                        new TopBorder(
                            new Color() { Auto = true }
                        )
                        { Style = BorderStyleValues.Thin },
                        new BottomBorder(
                            new Color() { Auto = true }
                        )
                        { Style = BorderStyleValues.Thin },
                        new DiagonalBorder())
                ),
                new CellFormats(
                    // Index 0 - Default cell style
                    new CellFormat()
                    {
                        FontId = 0,
                        FillId = 0,
                        BorderId = 0,
                        ApplyFont = true,
                        Alignment = new Alignment()
                        {
                            Horizontal = HorizontalAlignmentValues.Left,
                            Vertical = VerticalAlignmentValues.Center
                        }
                    },

                    // Index 1 - Bold Left Align
                    new CellFormat()
                    {
                        FontId = 1,
                        FillId = 0,
                        BorderId = 0,
                        ApplyFont = true,
                        Alignment = new Alignment()
                        {
                            Horizontal = HorizontalAlignmentValues.Left,
                            Vertical = VerticalAlignmentValues.Center
                        }
                    },

                    // Index 2 - Bold Right Align
                    new CellFormat()
                    {
                        FontId = 1,
                        FillId = 0,
                        BorderId = 0,
                        ApplyFont = true,
                        Alignment = new Alignment()
                        {
                            Horizontal = HorizontalAlignmentValues.Right,
                            Vertical = VerticalAlignmentValues.Center
                        }
                    },

                    // Index 3 - Align Center
                    new CellFormat()
                    {
                        FontId = 0,
                        FillId = 0,
                        BorderId = 0,
                        ApplyFont = true,
                        Alignment = new Alignment()
                        {
                            Horizontal = HorizontalAlignmentValues.Center,
                            Vertical = VerticalAlignmentValues.Center
                        }
                    },

                    // Index 4 - Black Header
                    new CellFormat()
                    {
                        FontId = 3,
                        FillId = 3,
                        BorderId = 0,
                        ApplyFill = true,
                        Alignment = new Alignment()
                        {
                            Horizontal = HorizontalAlignmentValues.Left,
                            Vertical = VerticalAlignmentValues.Center
                        }
                    },

                    // Index 5 - Right Align
                    new CellFormat()
                    {
                        FontId = 0,
                        FillId = 0,
                        BorderId = 0,
                        ApplyFont = true,
                        Alignment = new Alignment()
                        {
                            Horizontal = HorizontalAlignmentValues.Right,
                            Vertical = VerticalAlignmentValues.Center
                        }
                    },

                    // Index 6 - Left Align
                    new CellFormat()
                    {
                        FontId = 0,
                        FillId = 0,
                        BorderId = 0,
                        ApplyFont = true,
                        Alignment = new Alignment()
                        {
                            Horizontal = HorizontalAlignmentValues.Left,
                            Vertical = VerticalAlignmentValues.Center
                        }
                    }
                )
            );

            using (var stream = new MemoryStream())
            using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
            {
                // ------------------------------------------------------------
                // Add document properties.
                // ------------------------------------------------------------

                document.PackageProperties.Creator = "BRIZBEE";
                document.PackageProperties.Created = DateTime.UtcNow;


                // ------------------------------------------------------------
                // Add a WorkbookPart to the document.
                // ------------------------------------------------------------

                var workbookPart1 = document.AddWorkbookPart();
                workbookPart1.Workbook = new Workbook();


                // ------------------------------------------------------------
                // Apply stylesheet.
                // ------------------------------------------------------------

                var workStylePart1 = workbookPart1.AddNewPart<WorkbookStylesPart>();
                workStylePart1.Stylesheet = styleSheet;


                // ------------------------------------------------------------
                // Add a WorksheetPart to the WorkbookPart.
                // ------------------------------------------------------------

                var worksheetPart1 = workbookPart1.AddNewPart<WorksheetPart>();


                // ------------------------------------------------------------
                // Collect the users and punches.
                // ------------------------------------------------------------

                var users = db.Users
                    .Where(u => u.OrganizationId == currentUser.OrganizationId)
                    .Where(u => u.IsDeleted == false)
                    .OrderBy(u => u.Name)
                    .ToList();

                var punches = db.Punches
                    .Include(p => p.Task.Job.Customer)
                    .Include(p => p.User)
                    .Include(p => p.ServiceRate)
                    .Include(p => p.PayrollRate)
                    .Where(p => p.User.OrganizationId == currentUser.OrganizationId)
                    .Where(p => p.User.IsDeleted == false)
                    .Where(p => p.OutAt.HasValue == true)
                    .Where(p => DbFunctions.TruncateTime(p.InAt) >= min.Date)
                    .Where(p => DbFunctions.TruncateTime(p.InAt) <= max.Date)
                    .ToList();

                var rowIndex = (uint)1;
                var worksheet1 = new Worksheet();
                var rowBreaks1 = new RowBreaks() { Count = 0, ManualBreakCount = 0 };
                var mergeCells1 = new MergeCells() { Count = 0 };
                var sheetData1 = new SheetData();
                foreach (var user in users)
                {
                    var punchesForUser = punches
                        .Where(p => p.UserId == user.Id)
                        .OrderBy(p => p.InAt);

                    // Do not continue adding this user if there are no punches.
                    if (!punchesForUser.Any())
                        continue;

                    // ------------------------------------------------------------
                    // Header for user cell.
                    // ------------------------------------------------------------

                    var rowUser = new Row() { RowIndex = rowIndex, Height = 22D, CustomHeight = true, Spans = new ListValue<StringValue>() { InnerText = "1:1" }, StyleIndex = 4U, CustomFormat = true };

                    var cellUser = new Cell() { CellReference = $"A{rowIndex}", StyleIndex = 4U, DataType = CellValues.String, CellValue = new CellValue($"User {user.Name}") };
                    rowUser.Append(cellUser);

                    sheetData1.Append(rowUser);

                    // Merge the user name across the row.
                    var mergeCell0 = new MergeCell() { Reference = $"A{rowIndex}:L{rowIndex}" };
                    mergeCells1.Append(mergeCell0);
                    mergeCells1.Count++;

                    rowIndex++;


                    // ------------------------------------------------------------
                    // Loop each date.
                    // ------------------------------------------------------------

                    var dates = punches
                        .GroupBy(p => p.InAt.Date)
                        .Select(g => g.Key)
                        .ToList();
                    foreach (var date in dates)
                    {
                        var punchesForDate = punchesForUser
                            .Where(p => p.InAt.Date == date.Date);

                        if (!punchesForDate.Any())
                            continue;

                        rowIndex++;

                        // ------------------------------------------------------------
                        // Header for date cell.
                        // ------------------------------------------------------------

                        var rowDate = new Row() { RowIndex = rowIndex, Height = 22D, CustomHeight = true, Spans = new ListValue<StringValue>() { InnerText = "1:1" }, StyleIndex = 4U, CustomFormat = true };

                        var cellDate = new Cell() { CellReference = $"A{rowIndex}", StyleIndex = 4U, DataType = CellValues.String, CellValue = new CellValue(date.ToString("D")) };
                        rowDate.Append(cellDate);

                        sheetData1.Append(rowDate);

                        // Merge the date across the row.
                        var mergeCell1 = new MergeCell() { Reference = $"A{rowIndex}:L{rowIndex}" };
                        mergeCells1.Append(mergeCell1);
                        mergeCells1.Count++;

                        rowIndex++;


                        // ------------------------------------------------------------
                        // Headers for punch cells.
                        // ------------------------------------------------------------

                        var rowHeaders = new Row() { RowIndex = rowIndex, Height = 16D, CustomHeight = true, StyleIndex = 1U, CustomFormat = true };

                        // InAt
                        var cellInAtHeader = new Cell() { CellReference = $"A{rowIndex}", DataType = CellValues.String, StyleIndex = 1U };
                        var cellValueForInAtHeader = new CellValue("In");

                        cellInAtHeader.Append(cellValueForInAtHeader);
                        rowHeaders.Append(cellInAtHeader);

                        // OutAt
                        var cellOutAtHeader = new Cell() { CellReference = $"B{rowIndex}", DataType = CellValues.String, StyleIndex = 1U };
                        var cellValueForOutAtHeader = new CellValue("Out");

                        cellOutAtHeader.Append(cellValueForOutAtHeader);
                        rowHeaders.Append(cellOutAtHeader);

                        // Task Number
                        var cellTaskNumberHeader = new Cell() { CellReference = $"C{rowIndex}", DataType = CellValues.String, StyleIndex = 1U, CellValue = new CellValue("#") };
                        rowHeaders.Append(cellTaskNumberHeader);

                        // Task Name
                        var cellTaskNameHeader = new Cell() { CellReference = $"D{rowIndex}", DataType = CellValues.String, StyleIndex = 1U, CellValue = new CellValue("Task") };
                        rowHeaders.Append(cellTaskNameHeader);

                        // Project Number
                        var cellProjectNumberHeader = new Cell() { CellReference = $"E{rowIndex}", DataType = CellValues.String, StyleIndex = 1U, CellValue = new CellValue("#") };
                        rowHeaders.Append(cellProjectNumberHeader);

                        // Project Name
                        var cellProjectNameHeader = new Cell() { CellReference = $"F{rowIndex}", DataType = CellValues.String, StyleIndex = 1U, CellValue = new CellValue("Project") };
                        rowHeaders.Append(cellProjectNameHeader);

                        // Customer Number
                        var cellCustomerNumberHeader = new Cell() { CellReference = $"G{rowIndex}", DataType = CellValues.String, StyleIndex = 1U, CellValue = new CellValue("#") };
                        rowHeaders.Append(cellCustomerNumberHeader);

                        // Customer Name
                        var cellCustomerNameHeader = new Cell() { CellReference = $"H{rowIndex}", DataType = CellValues.String, StyleIndex = 1U, CellValue = new CellValue("Customer") };
                        rowHeaders.Append(cellCustomerNameHeader);

                        // Customer Rate
                        var cellCustomerRateHeader = new Cell() { CellReference = $"I{rowIndex}", DataType = CellValues.String, StyleIndex = 1U, CellValue = new CellValue("Customer Rate") };
                        rowHeaders.Append(cellCustomerRateHeader);

                        // Payroll Rate
                        var cellPayrollRateHeader = new Cell() { CellReference = $"J{rowIndex}", DataType = CellValues.String, StyleIndex = 1U, CellValue = new CellValue("Payroll Rate") };
                        rowHeaders.Append(cellPayrollRateHeader);

                        // Locked
                        var cellLockedHeader = new Cell() { CellReference = $"K{rowIndex}", DataType = CellValues.String, StyleIndex = 1U, CellValue = new CellValue("Locked?") };
                        rowHeaders.Append(cellLockedHeader);

                        // Total
                        var cellTotalHeader = new Cell() { CellReference = $"L{rowIndex}", DataType = CellValues.String, StyleIndex = 2U, CellValue = new CellValue("Total") };
                        rowHeaders.Append(cellTotalHeader);

                        sheetData1.Append(rowHeaders);

                        rowIndex++;


                        // ------------------------------------------------------------
                        // Punch cells.
                        // ------------------------------------------------------------

                        foreach (var punch in punchesForDate)
                        {
                            var rowPunch = new Row() { RowIndex = rowIndex, Height = 16D, CustomHeight = true };

                            // InAt
                            var cellInAt = new Cell() { CellReference = $"A{rowIndex}", DataType = CellValues.String, StyleIndex = 6U, CellValue = new CellValue(punch.InAt.ToShortTimeString()) };
                            rowPunch.Append(cellInAt);

                            // OutAt
                            var cellOutAt = new Cell() { CellReference = $"B{rowIndex}", DataType = CellValues.String, StyleIndex = 6U, CellValue = new CellValue(punch.OutAt.Value.ToShortTimeString()) };
                            rowPunch.Append(cellOutAt);

                            // Task Number
                            var cellTaskNumber = new Cell() { CellReference = $"C{rowIndex}", DataType = CellValues.Number, StyleIndex = 6U, CellValue = new CellValue(punch.Task.Number) };
                            rowPunch.Append(cellTaskNumber);

                            // Task Name
                            var cellTaskName = new Cell() { CellReference = $"D{rowIndex}", DataType = CellValues.String, StyleIndex = 6U, CellValue = new CellValue(punch.Task.Name) };
                            rowPunch.Append(cellTaskName);

                            // Project Number
                            var cellProjectNumber = new Cell() { CellReference = $"E{rowIndex}", DataType = CellValues.Number, StyleIndex = 6U, CellValue = new CellValue(punch.Task.Job.Number) };
                            rowPunch.Append(cellProjectNumber);

                            // Project Name
                            var cellProjectName = new Cell() { CellReference = $"F{rowIndex}", DataType = CellValues.String, StyleIndex = 6U, CellValue = new CellValue(punch.Task.Job.Name) };
                            rowPunch.Append(cellProjectName);

                            // Customer Number
                            var cellCustomerNumber = new Cell() { CellReference = $"G{rowIndex}", DataType = CellValues.Number, StyleIndex = 6U, CellValue = new CellValue(punch.Task.Job.Customer.Number) };
                            rowPunch.Append(cellCustomerNumber);

                            // Customer Name
                            var cellCustomerName = new Cell() { CellReference = $"H{rowIndex}", DataType = CellValues.String, StyleIndex = 6U, CellValue = new CellValue(punch.Task.Job.Customer.Name) };
                            rowPunch.Append(cellCustomerName);

                            // Customer Rate
                            var cellCustomerRate = new Cell() { CellReference = $"I{rowIndex}", DataType = CellValues.String, StyleIndex = 6U, CellValue = new CellValue(punch.ServiceRateId.HasValue ? punch.ServiceRate.Name : "") };
                            rowPunch.Append(cellCustomerRate);

                            // Payroll Rate
                            var cellPayrollRate = new Cell() { CellReference = $"J{rowIndex}", DataType = CellValues.String, StyleIndex = 6U, CellValue = new CellValue(punch.PayrollRateId.HasValue ? punch.PayrollRate.Name : "") };
                            rowPunch.Append(cellPayrollRate);

                            // Locked
                            var cellLocked = new Cell() { CellReference = $"K{rowIndex}", DataType = CellValues.String, StyleIndex = 3U, CellValue = new CellValue(punch.CommitId.HasValue ? "X" : "") };
                            rowPunch.Append(cellLocked);

                            // Calculate the total
                            var total = Math.Round((punch.OutAt.Value - punch.InAt).TotalMinutes / 60, 2).ToString("0.00");

                            // Total
                            var cellTotal = new Cell() { CellReference = $"L{rowIndex}", DataType = CellValues.Number, StyleIndex = 5U, CellValue = new CellValue(total) };
                            rowPunch.Append(cellTotal);

                            sheetData1.Append(rowPunch);

                            rowIndex++;
                        }

                        // ------------------------------------------------------------
                        // Cells for date total.
                        // ------------------------------------------------------------

                        var rowDateTotal = new Row() { RowIndex = rowIndex, Height = 16D, CustomHeight = true, StyleIndex = 1U, CustomFormat = true };

                        // Header Cell
                        var cellDateFormatted = new Cell() { CellReference = $"A{rowIndex}", DataType = CellValues.String, StyleIndex = 2U, CellValue = new CellValue("Daily Total") };
                        rowDateTotal.Append(cellDateFormatted);

                        // Calculate the total
                        double dailyTotalMinutes = 0;
                        foreach (var punch in punchesForDate)
                        {
                            dailyTotalMinutes += (punch.OutAt.Value - punch.InAt).TotalMinutes;
                        }
                        var dailyTotal = Math.Round(dailyTotalMinutes / 60, 2).ToString("0.00");

                        // Total Cell
                        var cellDateTotal = new Cell() { CellReference = $"L{rowIndex}", DataType = CellValues.Number, StyleIndex = 2U, CellValue = new CellValue(dailyTotal) };
                        rowDateTotal.Append(cellDateTotal);

                        sheetData1.Append(rowDateTotal);

                        // Merge the date across the row.
                        var mergeCell3 = new MergeCell() { Reference = $"A{rowIndex}:K{rowIndex}" };
                        mergeCells1.Append(mergeCell3);
                        mergeCells1.Count++;

                        rowIndex++;
                    }

                    
                    // ------------------------------------------------------------
                    // Cells for user total.
                    // ------------------------------------------------------------

                    var rowTotal = new Row() { RowIndex = rowIndex, Height = 16D, CustomHeight = true, StyleIndex = 1U, CustomFormat = true };

                    // Header Cell
                    var cellUserName = new Cell() { CellReference = $"A{rowIndex}", DataType = CellValues.String, StyleIndex = 2U, CellValue = new CellValue($"Total for User {user.Name}") };
                    rowTotal.Append(cellUserName);

                    // Calculate the total
                    double userTotalMinutes = 0;
                    foreach (var punch in punchesForUser)
                    {
                        userTotalMinutes += (punch.OutAt.Value - punch.InAt).TotalMinutes;
                    }
                    var userTotal = Math.Round(userTotalMinutes / 60, 2).ToString("0.00");

                    // Total Cell
                    var cellUserTotal = new Cell() { CellReference = $"L{rowIndex}", DataType = CellValues.Number, StyleIndex = 2U, CellValue = new CellValue(userTotal) };
                    rowTotal.Append(cellUserTotal);

                    sheetData1.Append(rowTotal);

                    // Merge the user name across the row.
                    var mergeCell4 = new MergeCell() { Reference = $"A{rowIndex}:K{rowIndex}" };
                    mergeCells1.Append(mergeCell4);
                    mergeCells1.Count++;

                    rowIndex++;


                    // ------------------------------------------------------------
                    // Add a page break.
                    // ------------------------------------------------------------

                    var rowBreak1 = new Break() { Id = rowIndex, Max = 16383U, ManualPageBreak = true };
                    rowBreaks1.Append(rowBreak1);
                    rowBreaks1.ManualBreakCount++;
                    rowBreaks1.Count++;

                    rowIndex++;
                }


                // ------------------------------------------------------------
                // Custom column width.
                // ------------------------------------------------------------

                var columns1 = new Columns();

                var column1 = new Column() { Min = 1U, Max = 1U, Width = 9D, CustomWidth = true };
                var column2 = new Column() { Min = 2U, Max = 2U, Width = 9D, CustomWidth = true };
                var column3 = new Column() { Min = 3U, Max = 3U, Width = 8D, CustomWidth = true };
                var column4 = new Column() { Min = 4U, Max = 4U, Width = 28D, CustomWidth = true };
                var column5 = new Column() { Min = 5U, Max = 5U, Width = 8D, CustomWidth = true };
                var column6 = new Column() { Min = 6U, Max = 6U, Width = 28D, CustomWidth = true };
                var column7 = new Column() { Min = 7U, Max = 7U, Width = 8D, CustomWidth = true };
                var column8 = new Column() { Min = 8U, Max = 8U, Width = 28D, CustomWidth = true };
                var column9 = new Column() { Min = 9U, Max = 9U, Width = 15D, CustomWidth = true };
                var column10 = new Column() { Min = 10U, Max = 10U, Width = 15D, CustomWidth = true };
                var column11 = new Column() { Min = 11U, Max = 11U, Width = 8D, CustomWidth = true };
                var column12 = new Column() { Min = 12U, Max = 12U, Width = 8D, CustomWidth = true };

                columns1.Append(column1);
                columns1.Append(column2);
                columns1.Append(column3);
                columns1.Append(column4);
                columns1.Append(column5);
                columns1.Append(column6);
                columns1.Append(column7);
                columns1.Append(column8);
                columns1.Append(column9);
                columns1.Append(column10);
                columns1.Append(column11);
                columns1.Append(column12);


                // ------------------------------------------------------------
                // Sheet Views.
                // ------------------------------------------------------------

                var sheetViews1 = new SheetViews();

                var sheetView1 = new SheetView() { ShowGridLines = true, TabSelected = true, ZoomScaleNormal = 100U, WorkbookViewId = 0U };
                var selection1 = new Selection() { ActiveCell = "A1", SequenceOfReferences = new ListValue<StringValue>() { InnerText = "A1" } };

                sheetView1.Append(selection1);
                sheetViews1.Append(sheetView1);


                // ------------------------------------------------------------
                // Sheet Format.
                // ------------------------------------------------------------

                var sheetFormatProperties1 = new SheetFormatProperties() { DefaultRowHeight = 16D, DyDescent = 0.35D };


                // ------------------------------------------------------------
                // Page Setup.
                // ------------------------------------------------------------

                var pageMargins1 = new PageMargins() { Left = 0.5D, Right = 0.5D, Top = 0.5D, Bottom = 0.5D, Header = 0.3D, Footer = 0.3D };
                var pageSetup1 = new PageSetup() { Orientation = OrientationValues.Landscape };


                // ------------------------------------------------------------
                // Header and Footer.
                // ------------------------------------------------------------

                var headerFooter1 = new HeaderFooter();

                var oddHeader1 = new OddHeader();
                oddHeader1.Text = reportTitle;

                var oddFooter1 = new OddFooter();
                oddFooter1.Text = organizationName;

                headerFooter1.Append(oddHeader1);
                headerFooter1.Append(oddFooter1);


                // ------------------------------------------------------------
                // Build the worksheet.
                // ------------------------------------------------------------

                worksheet1.Append(sheetViews1);
                worksheet1.Append(columns1);
                worksheet1.Append(sheetData1);
                worksheet1.Append(mergeCells1);
                worksheet1.Append(pageMargins1);
                worksheet1.Append(pageSetup1);
                worksheet1.Append(headerFooter1);

                worksheet1.Append(rowBreaks1);

                worksheetPart1.Worksheet = worksheet1;


                // ------------------------------------------------------------
                // Add Sheets to the Workbook.
                // ------------------------------------------------------------

                var sheets = workbookPart1.Workbook.AppendChild(new Sheets());


                // ------------------------------------------------------------
                // Append a new worksheet and associate it with the workbook.
                // ------------------------------------------------------------

                var sheet = new Sheet()
                {
                    Id = workbookPart1.GetIdOfPart(worksheetPart1),
                    SheetId = 1,
                    Name = "Report"
                };
                sheets.Append(sheet);


                // Save and close the document.
                workbookPart1.Workbook.Save();
                document.Close();

                return new FileActionResult(
                    stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Report - Punches for Payroll {min.ToString("yyyy_MM_dd")} thru {max.ToString("yyyy_MM_dd")}.xlsx",
                    Request);
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