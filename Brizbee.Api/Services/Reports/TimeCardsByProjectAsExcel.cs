//
//  TimeCardsByProjectAsExcel.cs
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

using Brizbee.Core.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;

namespace Brizbee.Api.Services.Reports
{
    public class TimeCardsByProjectAsExcel
    {
        public byte[] Build(SqlContext context, User currentUser, DateTime min, DateTime max, string filterUserScope, int[] filterUserIds, string filterProjectScope, int[] filterProjectIds)
        {
            var organization = context.Organizations.Find(currentUser.OrganizationId);

            var reportTitle = $"TIME CARDS BY PROJECT {min.ToString("M/d/yyyy")} thru {max.ToString("M/d/yyyy")} GENERATED {DateTime.Now.ToString("ddd, MMM d, yyyy h:mm:ss tt").ToUpperInvariant()}";

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
                workStylePart1.Stylesheet = Stylesheets.Common(); ;


                // ------------------------------------------------------------
                // Add a WorksheetPart to the WorkbookPart.
                // ------------------------------------------------------------

                var worksheetPart1 = workbookPart1.AddNewPart<WorksheetPart>();
                var worksheet1 = new Worksheet();
                var rowBreaks1 = new RowBreaks() { Count = 0, ManualBreakCount = 0 };
                var mergeCells1 = new MergeCells() { Count = 0 };
                var sheetData1 = new SheetData();


                // ------------------------------------------------------------
                // Collect the users based on the filters.
                // ------------------------------------------------------------

                List<User> users;
                if (filterUserScope.ToUpperInvariant() == "SPECIFIC")
                {
                    users = context.Users
                        .Where(u => u.OrganizationId == currentUser.OrganizationId)
                        .Where(u => u.IsDeleted == false)
                        .Where(u => filterUserIds.Contains(u.Id))
                        .OrderBy(u => u.Name)
                        .ToList();
                }
                else
                {
                    users = context.Users
                        .Where(u => u.OrganizationId == currentUser.OrganizationId)
                        .Where(u => u.IsDeleted == false)
                        .OrderBy(u => u.Name)
                        .ToList();
                }


                // ------------------------------------------------------------
                // Collect the time cards based on the filters.
                // ------------------------------------------------------------

                IQueryable<TimesheetEntry> timeCardsQueryable = context.TimesheetEntries
                    .Include(t => t.Task.Job.Customer)
                    .Include(t => t.User)
                    .Where(t => t.User.OrganizationId == currentUser.OrganizationId)
                    .Where(t => t.User.IsDeleted == false)
                    .Where(t => t.EnteredAt.Date >= min.Date)
                    .Where(t => t.EnteredAt.Date <= max.Date);

                // Optionally filter projects.
                if (filterProjectScope.ToUpperInvariant() == "SPECIFIC")
                {
                    timeCardsQueryable = timeCardsQueryable
                        .Where(t => filterProjectIds.Contains(t.Task.JobId));
                }

                var timeCards = timeCardsQueryable.ToList();

                var projectIds = timeCards
                    .OrderBy(t => t.Task.Job.Number)
                    .GroupBy(t => t.Task.JobId)
                    .Select(g => g.Key)
                    .ToList();


                // ------------------------------------------------------------
                // Loop each project.
                // ------------------------------------------------------------

                var rowIndex = (uint)1;
                foreach (var projectId in projectIds)
                {
                    var project = context.Jobs.Find(projectId);
                    var groupedTaskIds = timeCards
                        .GroupBy(t => t.TaskId)
                        .Select(g => g.Key)
                        .ToList();
                    var timeCardsForProject = timeCards
                        .Where(t => groupedTaskIds.Contains(t.TaskId))
                        .ToList();


                    // ------------------------------------------------------------
                    // Header for project cell.
                    // ------------------------------------------------------------

                    var rowProject = new Row() { RowIndex = rowIndex, Height = 22D, CustomHeight = true, Spans = new ListValue<StringValue>() { InnerText = "1:1" }, StyleIndex = 4U, CustomFormat = true };

                    var cellProject = new Cell() { CellReference = $"A{rowIndex}", StyleIndex = 4U, DataType = CellValues.String, CellValue = new CellValue($"Project {project.Number} - {project.Name} for Customer {project.Customer.Number} - {project.Customer.Name}") };
                    rowProject.Append(cellProject);

                    sheetData1.Append(rowProject);

                    // Merge the user name across the row.
                    var mergeCell0 = new MergeCell() { Reference = $"A{rowIndex}:E{rowIndex}" };
                    mergeCells1.Append(mergeCell0);
                    mergeCells1.Count++;

                    rowIndex++;


                    // ------------------------------------------------------------
                    // Loop each date.
                    // ------------------------------------------------------------

                    var dates = timeCards
                        .GroupBy(t => t.EnteredAt.Date)
                        .Select(g => g.Key)
                        .OrderBy(g => g)
                        .ToList();
                    foreach (var date in dates)
                    {
                        var timeCardsForProjectAndDate = timeCards
                            .Where(t => t.EnteredAt.Date == date.Date)
                            .Where(t => groupedTaskIds.Contains(t.TaskId))
                            .OrderBy(t => t.EnteredAt)
                            .ToList();

                        if (!timeCardsForProjectAndDate.Any())
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
                        var mergeCell1 = new MergeCell() { Reference = $"A{rowIndex}:E{rowIndex}" };
                        mergeCells1.Append(mergeCell1);
                        mergeCells1.Count++;

                        rowIndex++;


                        // ------------------------------------------------------------
                        // Headers for time cards cells.
                        // ------------------------------------------------------------

                        var rowHeaders = new Row() { RowIndex = rowIndex, Height = 16D, CustomHeight = true, StyleIndex = 1U, CustomFormat = true };

                        // Task Number
                        var cellTaskNumberHeader = new Cell() { CellReference = $"A{rowIndex}", DataType = CellValues.String, StyleIndex = 1U, CellValue = new CellValue("#") };
                        rowHeaders.Append(cellTaskNumberHeader);

                        // Task Name
                        var cellTaskNameHeader = new Cell() { CellReference = $"B{rowIndex}", DataType = CellValues.String, StyleIndex = 1U, CellValue = new CellValue("Task") };
                        rowHeaders.Append(cellTaskNameHeader);

                        // User Name
                        var cellUserNameHeader = new Cell() { CellReference = $"C{rowIndex}", DataType = CellValues.String, StyleIndex = 1U, CellValue = new CellValue("User") };
                        rowHeaders.Append(cellUserNameHeader);

                        // Notes
                        var cellNotesHeader = new Cell() { CellReference = $"D{rowIndex}", DataType = CellValues.String, StyleIndex = 1U, CellValue = new CellValue("Notes") };
                        rowHeaders.Append(cellNotesHeader);

                        // Total
                        var cellTotalHeader = new Cell() { CellReference = $"E{rowIndex}", DataType = CellValues.String, StyleIndex = 2U, CellValue = new CellValue("Total") };
                        rowHeaders.Append(cellTotalHeader);

                        sheetData1.Append(rowHeaders);

                        rowIndex++;


                        // ------------------------------------------------------------
                        // Time cards cells.
                        // ------------------------------------------------------------

                        foreach (var timeCard in timeCardsForProjectAndDate)
                        {
                            var rowTimeCard = new Row() { RowIndex = rowIndex, Height = 16D, CustomHeight = true };

                            // Task Number
                            var cellTaskNumber = new Cell() { CellReference = $"A{rowIndex}", DataType = CellValues.Number, StyleIndex = 6U, CellValue = new CellValue(timeCard.Task.Number) };
                            rowTimeCard.Append(cellTaskNumber);

                            // Task Name
                            var cellTaskName = new Cell() { CellReference = $"B{rowIndex}", DataType = CellValues.String, StyleIndex = 6U, CellValue = new CellValue(timeCard.Task.Name) };
                            rowTimeCard.Append(cellTaskName);

                            // User Name
                            var cellUserName = new Cell() { CellReference = $"C{rowIndex}", DataType = CellValues.String, StyleIndex = 6U, CellValue = new CellValue(timeCard.User.Name) };
                            rowTimeCard.Append(cellUserName);

                            // Notes
                            var cellNotes = new Cell() { CellReference = $"D{rowIndex}", DataType = CellValues.String, StyleIndex = 6U, CellValue = new CellValue(timeCard.Notes) };
                            rowTimeCard.Append(cellNotes);

                            // Calculate the total
                            var total = Math.Round((double)timeCard.Minutes / 60, 2).ToString("0.00");

                            // Total
                            var cellTotal = new Cell() { CellReference = $"E{rowIndex}", DataType = CellValues.Number, StyleIndex = 5U, CellValue = new CellValue(total) };
                            rowTimeCard.Append(cellTotal);

                            sheetData1.Append(rowTimeCard);

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
                        foreach (var timeCard in timeCardsForProjectAndDate)
                        {
                            dailyTotalMinutes += timeCard.Minutes;
                        }
                        var dailyTotal = Math.Round(dailyTotalMinutes / 60, 2).ToString("0.00");

                        // Total Cell
                        var cellDateTotal = new Cell() { CellReference = $"E{rowIndex}", DataType = CellValues.Number, StyleIndex = 2U, CellValue = new CellValue(dailyTotal) };
                        rowDateTotal.Append(cellDateTotal);

                        sheetData1.Append(rowDateTotal);

                        // Merge the date across the row.
                        var mergeCell3 = new MergeCell() { Reference = $"A{rowIndex}:D{rowIndex}" };
                        mergeCells1.Append(mergeCell3);
                        mergeCells1.Count++;

                        rowIndex++;
                    }


                    // ------------------------------------------------------------
                    // Cells for project total.
                    // ------------------------------------------------------------

                    var rowTotal = new Row() { RowIndex = rowIndex, Height = 16D, CustomHeight = true, StyleIndex = 1U, CustomFormat = true };

                    // Header Cell
                    var cellProjectName = new Cell() { CellReference = $"A{rowIndex}", DataType = CellValues.String, StyleIndex = 2U, CellValue = new CellValue($"Total for Project {project.Number} - {project.Name}") };
                    rowTotal.Append(cellProjectName);

                    // Calculate the total
                    double projectTotalMinutes = 0;
                    foreach (var timeCard in timeCardsForProject)
                    {
                        projectTotalMinutes += timeCard.Minutes;
                    }
                    var projectTotal = Math.Round(projectTotalMinutes / 60, 2).ToString("0.00");

                    // Total Cell
                    var cellProjectTotal = new Cell() { CellReference = $"E{rowIndex}", DataType = CellValues.Number, StyleIndex = 2U, CellValue = new CellValue(projectTotal) };
                    rowTotal.Append(cellProjectTotal);

                    sheetData1.Append(rowTotal);

                    // Merge the project name across the row.
                    var mergeCell4 = new MergeCell() { Reference = $"A{rowIndex}:D{rowIndex}" };
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

                var column1 = new Column() { Min = 1U, Max = 1U, Width = 10D, CustomWidth = true };
                var column2 = new Column() { Min = 2U, Max = 2U, Width = 28D, CustomWidth = true };
                var column3 = new Column() { Min = 3U, Max = 3U, Width = 28D, CustomWidth = true };
                var column4 = new Column() { Min = 4U, Max = 4U, Width = 40D, CustomWidth = true };
                var column5 = new Column() { Min = 5U, Max = 5U, Width = 10D, CustomWidth = true };

                columns1.Append(column1);
                columns1.Append(column2);
                columns1.Append(column3);
                columns1.Append(column4);
                columns1.Append(column5);


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
                oddFooter1.Text = organization.Name;

                headerFooter1.Append(oddHeader1);
                headerFooter1.Append(oddFooter1);


                // ------------------------------------------------------------
                // Build the worksheet.
                // ------------------------------------------------------------

                worksheet1.Append(sheetViews1);
                worksheet1.Append(columns1);
                worksheet1.Append(sheetData1);

                // Cannot add zero merge cells.
                if (mergeCells1.Count != 0)
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
                document.Save();

                return stream.ToArray();
            }
        }
    }
}