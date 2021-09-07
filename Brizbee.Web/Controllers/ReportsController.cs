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
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
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
            var job = db.Jobs.Where(j => j.Id == JobId).FirstOrDefault();
            var bytes = new ReportBuilder().TasksByJobAsPdf(JobId, CurrentUser(), taskGroupScope);
            return new FileActionResult(bytes, "application/pdf",
                string.Format(
                    "Tasks by Job for {0} - {1}.pdf",
                    job.Number,
                    job.Name),
                Request);
        }

        public IHttpActionResult PunchesForPayroll()
        {
            // Open the document for editing.
            using (SpreadsheetDocument document = SpreadsheetDocument.Open("document name", true))
            {
                //var relationshipId = "rId1";


                // ------------------------------------------------------------
                // Build Workbook Part.
                // ------------------------------------------------------------

                var workbookPart = document.AddWorkbookPart();
                var workbook = new Workbook();
                //var sheets = new Sheets();
                //var sheet1 = new Sheet() { Name = "First Sheet", SheetId = 1, Id = relationshipId };
                //sheets.Append(sheet1);
                //workbook.Append(sheets);
                workbookPart.Workbook = workbook;


                // ------------------------------------------------------------
                // Insert a new worksheet.
                // ------------------------------------------------------------

                WorksheetPart worksheetPart = InsertWorksheet(document.WorkbookPart);


                // ------------------------------------------------------------
                // Get the SharedStringTablePart. If it does not exist, create a new one.
                // ------------------------------------------------------------

                SharedStringTablePart sharedStringPart;
                if (document.WorkbookPart.GetPartsOfType<SharedStringTablePart>().Count() > 0)
                {
                    sharedStringPart = document.WorkbookPart.GetPartsOfType<SharedStringTablePart>().First();
                }
                else
                {
                    sharedStringPart = document.WorkbookPart.AddNewPart<SharedStringTablePart>();
                }


                // ------------------------------------------------------------
                // Insert the text into the SharedStringTablePart.
                // ------------------------------------------------------------

                int index = InsertSharedStringItem("my text", sharedStringPart);

                // Insert cell A1 into the new worksheet.
                Cell cell = InsertCellInWorksheet("A", 1, worksheetPart);

                // Set the value of cell A1.
                cell.CellValue = new CellValue(index.ToString());
                cell.DataType = new EnumValue<CellValues>(CellValues.SharedString);


                // ------------------------------------------------------------
                // Add document properties.
                // ------------------------------------------------------------

                document.PackageProperties.Creator = "BRIZBEE";
                document.PackageProperties.Created = DateTime.UtcNow;

                // Save the new worksheet.
                worksheetPart.Worksheet.Save();
            }

            return new FileActionResult(new byte[] { }, "application/pdf", "filename.xlsx", Request);
        }

        // Given text and a SharedStringTablePart, creates a SharedStringItem with the specified text 
        // and inserts it into the SharedStringTablePart. If the item already exists, returns its index.
        private static int InsertSharedStringItem(string text, SharedStringTablePart shareStringPart)
        {
            // If the part does not contain a SharedStringTable, create one.
            if (shareStringPart.SharedStringTable == null)
            {
                shareStringPart.SharedStringTable = new SharedStringTable();
            }

            int i = 0;

            // Iterate through all the items in the SharedStringTable. If the text already exists, return its index.
            foreach (SharedStringItem item in shareStringPart.SharedStringTable.Elements<SharedStringItem>())
            {
                if (item.InnerText == text)
                {
                    return i;
                }

                i++;
            }

            // The text does not exist in the part. Create the SharedStringItem and return its index.
            shareStringPart.SharedStringTable.AppendChild(new SharedStringItem(new DocumentFormat.OpenXml.Spreadsheet.Text(text)));
            shareStringPart.SharedStringTable.Save();

            return i;
        }

        // Given a WorkbookPart, inserts a new worksheet.
        private static WorksheetPart InsertWorksheet(WorkbookPart workbookPart)
        {
            // Add a new worksheet part to the workbook.
            WorksheetPart newWorksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            newWorksheetPart.Worksheet = new Worksheet(new SheetData());
            newWorksheetPart.Worksheet.Save();

            Sheets sheets = workbookPart.Workbook.GetFirstChild<Sheets>();
            string relationshipId = workbookPart.GetIdOfPart(newWorksheetPart);

            // Get a unique ID for the new sheet.
            uint sheetId = 1;
            if (sheets.Elements<Sheet>().Count() > 0)
            {
                sheetId = sheets.Elements<Sheet>().Select(s => s.SheetId.Value).Max() + 1;
            }

            string sheetName = "Sheet" + sheetId;

            // Append the new worksheet and associate it with the workbook.
            Sheet sheet = new Sheet() { Id = relationshipId, SheetId = sheetId, Name = sheetName };
            sheets.Append(sheet);
            workbookPart.Workbook.Save();

            return newWorksheetPart;
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
    }
}