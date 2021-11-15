//
//  PunchesByProjectAsExcel.cs
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
    public class PunchesByProjectAsExcel
    {
        public byte[] Build(SqlContext context, User currentUser, DateTime min, DateTime max, string filterUserScope, int[] filterUserIds, string filterProjectScope, int[] filterProjectIds, string filterLockStatus)
        {
            var organization = context.Organizations.Find(currentUser.OrganizationId);

            var reportTitle = $"PUNCHES BY PROJECT {min.ToString("M/d/yyyy")} thru {max.ToString("M/d/yyyy")} GENERATED {DateTime.Now.ToString("ddd, MMM d, yyyy h:mm:ss tt").ToUpperInvariant()}";

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
                // Collect the punches based on the filters.
                // ------------------------------------------------------------

                IQueryable<Punch> punchesQueryable = context.Punches
                    .Include(p => p.Task.Job.Customer)
                    .Include(p => p.User)
                    .Include(p => p.ServiceRate)
                    .Include(p => p.PayrollRate)
                    .Where(p => p.User.OrganizationId == currentUser.OrganizationId)
                    .Where(p => p.User.IsDeleted == false)
                    .Where(p => p.OutAt.HasValue == true)
                    .Where(p => p.InAt.Date >= min.Date)
                    .Where(p => p.InAt.Date <= max.Date);

                // Optionally filter projects.
                if (filterProjectScope.ToUpperInvariant() == "SPECIFIC")
                {
                    punchesQueryable = punchesQueryable
                        .Where(p => filterProjectIds.Contains(p.Task.JobId));
                }

                // Optionally filter locked or unlocked.
                if (filterLockStatus.ToUpperInvariant() == "ONLY")
                {
                    punchesQueryable = punchesQueryable.Where(p => p.CommitId != null);
                }
                else if (filterLockStatus.ToUpperInvariant() == "UNCOMMITTED")
                {
                    punchesQueryable = punchesQueryable.Where(p => p.CommitId == null);
                }

                var punches = punchesQueryable.ToList();

                var projectIds = punches
                    .OrderBy(p => p.Task.Job.Number)
                    .GroupBy(p => p.Task.JobId)
                    .Select(g => g.Key)
                    .ToList();


                // ------------------------------------------------------------
                // Loop each project.
                // ------------------------------------------------------------

                var rowIndex = (uint)1;
                foreach (var projectId in projectIds)
                {
                    var project = context.Jobs.Find(projectId);
                    var groupedTaskIds = punches
                        .GroupBy(p => p.TaskId)
                        .Select(g => g.Key)
                        .ToList();
                    var punchesForProject = punches
                        .Where(p => groupedTaskIds.Contains(p.TaskId))
                        .ToList();


                    // ------------------------------------------------------------
                    // Header for project cell.
                    // ------------------------------------------------------------

                    var rowProject = new Row() { RowIndex = rowIndex, Height = 22D, CustomHeight = true, Spans = new ListValue<StringValue>() { InnerText = "1:1" }, StyleIndex = 4U, CustomFormat = true };

                    var cellProject = new Cell() { CellReference = $"A{rowIndex}", StyleIndex = 4U, DataType = CellValues.String, CellValue = new CellValue($"Project {project.Number} - {project.Name} for Customer {project.Customer.Number} - {project.Customer.Name}") };
                    rowProject.Append(cellProject);

                    sheetData1.Append(rowProject);

                    // Merge the project name across the row.
                    var mergeCell0 = new MergeCell() { Reference = $"A{rowIndex}:I{rowIndex}" };
                    mergeCells1.Append(mergeCell0);
                    mergeCells1.Count++;

                    rowIndex++;


                    // ------------------------------------------------------------
                    // Loop each date.
                    // ------------------------------------------------------------

                    var dates = punches
                        .GroupBy(p => p.InAt.Date)
                        .Select(g => g.Key)
                        .OrderBy(g => g)
                        .ToList();
                    foreach (var date in dates)
                    {
                        var punchesForProjectAndDate = punches
                            .Where(p => p.InAt.Date == date.Date)
                            .Where(p => groupedTaskIds.Contains(p.TaskId))
                            .OrderBy(p => p.InAt)
                            .ToList();

                        if (!punchesForProjectAndDate.Any())
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
                        var mergeCell1 = new MergeCell() { Reference = $"A{rowIndex}:I{rowIndex}" };
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

                        // User Name
                        var cellUserNameHeader = new Cell() { CellReference = $"E{rowIndex}", DataType = CellValues.String, StyleIndex = 1U, CellValue = new CellValue("User") };
                        rowHeaders.Append(cellUserNameHeader);

                        // Customer Rate
                        var cellCustomerRateHeader = new Cell() { CellReference = $"F{rowIndex}", DataType = CellValues.String, StyleIndex = 1U, CellValue = new CellValue("Customer Rate") };
                        rowHeaders.Append(cellCustomerRateHeader);

                        // Payroll Rate
                        var cellPayrollRateHeader = new Cell() { CellReference = $"G{rowIndex}", DataType = CellValues.String, StyleIndex = 1U, CellValue = new CellValue("Payroll Rate") };
                        rowHeaders.Append(cellPayrollRateHeader);

                        // Locked
                        var cellLockedHeader = new Cell() { CellReference = $"H{rowIndex}", DataType = CellValues.String, StyleIndex = 1U, CellValue = new CellValue("Locked?") };
                        rowHeaders.Append(cellLockedHeader);

                        // Total
                        var cellTotalHeader = new Cell() { CellReference = $"I{rowIndex}", DataType = CellValues.String, StyleIndex = 2U, CellValue = new CellValue("Total") };
                        rowHeaders.Append(cellTotalHeader);

                        sheetData1.Append(rowHeaders);

                        rowIndex++;


                        // ------------------------------------------------------------
                        // Punch cells.
                        // ------------------------------------------------------------

                        foreach (var punch in punchesForProjectAndDate)
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

                            // User Name
                            var cellUserName = new Cell() { CellReference = $"E{rowIndex}", DataType = CellValues.String, StyleIndex = 6U, CellValue = new CellValue(punch.User.Name) };
                            rowPunch.Append(cellUserName);

                            // Customer Rate
                            var cellCustomerRate = new Cell() { CellReference = $"F{rowIndex}", DataType = CellValues.String, StyleIndex = 6U, CellValue = new CellValue(punch.ServiceRateId.HasValue ? punch.ServiceRate.Name : "") };
                            rowPunch.Append(cellCustomerRate);

                            // Payroll Rate
                            var cellPayrollRate = new Cell() { CellReference = $"G{rowIndex}", DataType = CellValues.String, StyleIndex = 6U, CellValue = new CellValue(punch.PayrollRateId.HasValue ? punch.PayrollRate.Name : "") };
                            rowPunch.Append(cellPayrollRate);

                            // Locked
                            var cellLocked = new Cell() { CellReference = $"H{rowIndex}", DataType = CellValues.String, StyleIndex = 3U, CellValue = new CellValue(punch.CommitId.HasValue ? "X" : "") };
                            rowPunch.Append(cellLocked);

                            // Calculate the total
                            var total = Math.Round((punch.OutAt.Value - punch.InAt).TotalMinutes / 60, 2).ToString("0.00");

                            // Total
                            var cellTotal = new Cell() { CellReference = $"I{rowIndex}", DataType = CellValues.Number, StyleIndex = 5U, CellValue = new CellValue(total) };
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
                        foreach (var punch in punchesForProjectAndDate)
                        {
                            dailyTotalMinutes += (punch.OutAt.Value - punch.InAt).TotalMinutes;
                        }
                        var dailyTotal = Math.Round(dailyTotalMinutes / 60, 2).ToString("0.00");

                        // Total Cell
                        var cellDateTotal = new Cell() { CellReference = $"I{rowIndex}", DataType = CellValues.Number, StyleIndex = 2U, CellValue = new CellValue(dailyTotal) };
                        rowDateTotal.Append(cellDateTotal);

                        sheetData1.Append(rowDateTotal);

                        // Merge the date across the row.
                        var mergeCell3 = new MergeCell() { Reference = $"A{rowIndex}:H{rowIndex}" };
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
                    foreach (var punch in punchesForProject)
                    {
                        projectTotalMinutes += (punch.OutAt.Value - punch.InAt).TotalMinutes;
                    }
                    var projectTotal = Math.Round(projectTotalMinutes / 60, 2).ToString("0.00");

                    // Total Cell
                    var cellProjectTotal = new Cell() { CellReference = $"I{rowIndex}", DataType = CellValues.Number, StyleIndex = 2U, CellValue = new CellValue(projectTotal) };
                    rowTotal.Append(cellProjectTotal);

                    sheetData1.Append(rowTotal);

                    // Merge the project name across the row.
                    var mergeCell4 = new MergeCell() { Reference = $"A{rowIndex}:H{rowIndex}" };
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

                var column1 = new Column() { Min = 1U, Max = 1U, Width = 12D, CustomWidth = true };
                var column2 = new Column() { Min = 2U, Max = 2U, Width = 12D, CustomWidth = true };
                var column3 = new Column() { Min = 3U, Max = 3U, Width = 10D, CustomWidth = true };
                var column4 = new Column() { Min = 4U, Max = 4U, Width = 28D, CustomWidth = true };
                var column5 = new Column() { Min = 5U, Max = 5U, Width = 28D, CustomWidth = true };
                var column6 = new Column() { Min = 6U, Max = 6U, Width = 24D, CustomWidth = true };
                var column7 = new Column() { Min = 7U, Max = 7U, Width = 24D, CustomWidth = true };
                var column8 = new Column() { Min = 8U, Max = 8U, Width = 10D, CustomWidth = true };
                var column9 = new Column() { Min = 9U, Max = 9U, Width = 15D, CustomWidth = true };

                columns1.Append(column1);
                columns1.Append(column2);
                columns1.Append(column3);
                columns1.Append(column4);
                columns1.Append(column5);
                columns1.Append(column6);
                columns1.Append(column7);
                columns1.Append(column8);
                columns1.Append(column9);


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
                document.Close();

                return stream.ToArray();
            }
        }
    }
}