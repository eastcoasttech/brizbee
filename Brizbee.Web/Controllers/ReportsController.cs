﻿//
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
        [AllowAnonymous]
        [Route("api/Reports/PunchesForPayroll")]
        public IHttpActionResult GetPunchesForPayroll()
        {
            //var currentUser = CurrentUser();

            //// Ensure that user is authorized.
            //if (!currentUser.CanViewReports)
            //    return StatusCode(HttpStatusCode.Forbidden);

            var styleSheet = new Stylesheet(
                new Fonts(
                    new Font(                                                               // Index 0 - The default font.
                        new FontSize() { Val = 11 },
                        new Color() { Rgb = new HexBinaryValue() { Value = "00000000" } },
                        new FontName() { Val = "Calibri" }),
                    new Font(                                                               // Index 1 - The bold font.
                        new Bold(),
                        new FontSize() { Val = 11 },
                        new Color() { Rgb = new HexBinaryValue() { Value = "00000000" } },
                        new FontName() { Val = "Calibri" }),
                    new Font(                                                               // Index 2 - The Italic font.
                        new Italic(),
                        new FontSize() { Val = 11 },
                        new Color() { Rgb = new HexBinaryValue() { Value = "00000000" } },
                        new FontName() { Val = "Calibri" }),
                    new Font(                                                               // Index 2 - The Times Roman font. with 16 size
                        new FontSize() { Val = 16 },
                        new Color() { Rgb = new HexBinaryValue() { Value = "00000000" } },
                        new FontName() { Val = "Times New Roman" })
                ),
                new Fills(
                    new Fill(                                                           // Index 0 - The default fill.
                        new PatternFill() { PatternType = PatternValues.None }),
                    new Fill(                                                           // Index 1 - The default fill of gray 125 (required)
                        new PatternFill() { PatternType = PatternValues.Gray125 }),
                    new Fill(                                                           // Index 2 - The yellow fill.
                        new PatternFill(
                            new ForegroundColor() { Rgb = new HexBinaryValue() { Value = "FFFFFF00" } }
                        )
                        { PatternType = PatternValues.Solid })
                ),
                new Borders(
                    new Border(                                                         // Index 0 - The default border.
                        new LeftBorder(),
                        new RightBorder(),
                        new TopBorder(),
                        new BottomBorder(),
                        new DiagonalBorder()),
                    new Border(                                                         // Index 1 - Applies a Left, Right, Top, Bottom border to a cell
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
                    new CellFormat() { FontId = 0, FillId = 0, BorderId = 0 },                          // Index 0 - The default cell style.  If a cell does not have a style index applied it will use this style combination instead
                    new CellFormat() { FontId = 1, FillId = 0, BorderId = 0, ApplyFont = true },       // Index 1 - Bold 
                    new CellFormat() { FontId = 2, FillId = 0, BorderId = 0, ApplyFont = true },       // Index 2 - Italic
                    new CellFormat() { FontId = 3, FillId = 0, BorderId = 0, ApplyFont = true },       // Index 3 - Times Roman
                    new CellFormat() { FontId = 0, FillId = 2, BorderId = 0, ApplyFill = true },       // Index 4 - Yellow Fill
                    new CellFormat(                                                                   // Index 5 - Alignment
                        new Alignment() { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center }
                    )
                    { FontId = 0, FillId = 0, BorderId = 0, ApplyAlignment = true },
                    new CellFormat() { FontId = 0, FillId = 0, BorderId = 1, ApplyBorder = true }      // Index 6 - Border
                )
            );

            using (var stream = new MemoryStream())
            using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
            {
                // ------------------------------------------------------------
                // Add a WorkbookPart to the document.
                // ------------------------------------------------------------

                var workbookpart = document.AddWorkbookPart();
                workbookpart.Workbook = new Workbook();


                // ------------------------------------------------------------
                // Apply stylesheet.
                // ------------------------------------------------------------

                var workStylePart = workbookpart.AddNewPart<WorkbookStylesPart>();
                workStylePart.Stylesheet = styleSheet;


                // ------------------------------------------------------------
                // Find or create the SharedStringTablePart.
                // ------------------------------------------------------------

                //SharedStringTablePart sharedStringPart;
                //if (workbookpart.GetPartsOfType<SharedStringTablePart>().Count() > 0)
                //{
                //    sharedStringPart = workbookpart.GetPartsOfType<SharedStringTablePart>().First();
                //}
                //else
                //{
                //    sharedStringPart = workbookpart.AddNewPart<SharedStringTablePart>();
                //}


                // ------------------------------------------------------------
                // Add a WorksheetPart to the WorkbookPart.
                // ------------------------------------------------------------

                var worksheetPart = workbookpart.AddNewPart<WorksheetPart>();
                var sheetProperties = new SheetProperties(new PageSetupProperties());
                worksheetPart.Worksheet = new Worksheet(new SheetData());
                worksheetPart.Worksheet.SheetProperties = sheetProperties;




                // Set the FitToPage property to true
                //worksheetPart.Worksheet.SheetProperties.PageSetupProperties.FitToPage = BooleanValue.FromBoolean(true);

                var pgOr = new PageSetup();
                pgOr.Orientation = OrientationValues.Landscape;
                worksheetPart.Worksheet.AppendChild(pgOr);













                // ------------------------------------------------------------
                // Add Sheets to the Workbook.
                // ------------------------------------------------------------

                var sheets = document.WorkbookPart.Workbook.AppendChild(new Sheets());


                // ------------------------------------------------------------
                // Append a new worksheet and associate it with the workbook.
                // ------------------------------------------------------------

                var sheet = new Sheet()
                {
                    Id = document.WorkbookPart.
                    GetIdOfPart(worksheetPart),
                    SheetId = 1,
                    Name = "Report"
                };
                sheets.Append(sheet);


                // ------------------------------------------------------------
                // Loop each user.
                // ------------------------------------------------------------

                var users = db.Users
                    .Where(u => u.OrganizationId == 26)
                    .ToList();

                var rowIndex = (uint)1;
                foreach (var user in users)
                {
                    var rowBreak = new Break() { Id = rowIndex, Max = 16383u, ManualPageBreak = true };

                    var rowBreaks = worksheetPart.Worksheet.GetFirstChild<RowBreaks>();

                    if (rowBreaks == null)
                    {
                        rowBreaks = new RowBreaks();
                        rowBreaks.ManualBreakCount = 0u;
                        rowBreaks.Count = 0u;

                        worksheetPart.Worksheet.Append(rowBreaks);
                    }

                    rowBreaks.Append(rowBreak);
                    rowBreaks.ManualBreakCount++;
                    rowBreaks.Count++;

                    // Set the value of the cell.
                    var cellA1 = InsertCellInWorksheet("A", rowIndex, worksheetPart);
                    cellA1.CellValue = new CellValue(user.Name);
                    cellA1.DataType = new EnumValue<CellValues>(CellValues.String);
                    cellA1.StyleIndex = 4u;

                    rowIndex++;
                }


                // ------------------------------------------------------------
                // Add document properties.
                // ------------------------------------------------------------

                document.PackageProperties.Creator = "BRIZBEE";
                document.PackageProperties.Created = DateTime.UtcNow;

                workbookpart.Workbook.Save();













                //// ------------------------------------------------------------
                //// Insert the text into the SharedStringTablePart.
                //// ------------------------------------------------------------

                //int index = InsertSharedStringItem("my text", sharedStringPart);

                //// Insert cell A1 into the new worksheet.
                //Cell cell = InsertCellInWorksheet("A", 1, worksheetPart);

                //// Set the value of cell A1.
                //cell.CellValue = new CellValue(index.ToString());
                //cell.DataType = new EnumValue<CellValues>(CellValues.SharedString);



                //// Save the new worksheet.
                //worksheetPart.Worksheet.Save();















                // Close the document.
                document.Close();

                return new FileActionResult(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Report - Punches for Payroll.xlsx", Request);
            }
        }

        // Given a column name, a row index, and a WorksheetPart, inserts a cell into the worksheet. 
        // If the cell already exists, returns it. 
        private static Cell InsertCellInWorksheet(string columnName, uint rowIndex, WorksheetPart worksheetPart)
        {
            Worksheet worksheet = worksheetPart.Worksheet;
            SheetData sheetData = worksheet.GetFirstChild<SheetData>();
            string cellReference = columnName + rowIndex;

            // If the worksheet does not contain a row with the specified row index, insert one.
            Row row;
            if (sheetData.Elements<Row>().Where(r => r.RowIndex == rowIndex).Count() != 0)
            {
                row = sheetData.Elements<Row>().Where(r => r.RowIndex == rowIndex).First();
            }
            else
            {
                row = new Row() { RowIndex = rowIndex };
                sheetData.Append(row);
            }

            // If there is not a cell with the specified column name, insert one.  
            if (row.Elements<Cell>().Where(c => c.CellReference.Value == columnName + rowIndex).Count() > 0)
            {
                return row.Elements<Cell>().Where(c => c.CellReference.Value == cellReference).First();
            }
            else
            {
                // Cells must be in sequential order according to CellReference. Determine where to insert the new cell.
                Cell refCell = null;
                foreach (Cell cell in row.Elements<Cell>())
                {
                    if (cell.CellReference.Value.Length == cellReference.Length)
                    {
                        if (string.Compare(cell.CellReference.Value, cellReference, true) > 0)
                        {
                            refCell = cell;
                            break;
                        }
                    }
                }

                Cell newCell = new Cell() { CellReference = cellReference };
                row.InsertBefore(newCell, refCell);

                worksheet.Save();
                return newCell;
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