using Brizbee.Common.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web.Hosting;

namespace Brizbee.Services
{
    public class ReportBuilder
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();
        private Font fontH1 = new Font(Font.FontFamily.HELVETICA, 9, Font.BOLD, BaseColor.WHITE);
        private Font fontH2 = new Font(Font.FontFamily.HELVETICA, 9, Font.BOLDITALIC);
        private Font fontH3 = new Font(Font.FontFamily.HELVETICA, 8, Font.BOLD);
        private Font fontP = new Font(Font.FontFamily.HELVETICA, 8, Font.NORMAL);
        private Font fontFooter = new Font(Font.FontFamily.HELVETICA, 9, Font.NORMAL);
        private float pageWidth = 0f;

        public byte[] PunchesByUserAsPdf(int[] userIds, DateTime min, DateTime max, User currentUser)
        {
            var buffer = new byte[0];
            var output = new MemoryStream();
            var organization = db.Organizations.Find(currentUser.OrganizationId);
            var tz = TimeZoneInfo.FindSystemTimeZoneById(organization.TimeZone);

            // Create an instance of document which represents the PDF document itself
            using (var document = new Document(PageSize.LETTER.Rotate(), 30, 30, 60, 60))
            {
                var writer = PdfWriter.GetInstance(document, output);
                pageWidth = document.PageSize.Width;

                // Add meta information to the document
                document.AddAuthor(currentUser.Name);
                document.AddCreator("BRIZBEE");
                document.AddTitle(string.Format(
                    "Punches by User {0} thru {1}.pdf",
                    TimeZoneInfo.ConvertTime(min, tz).ToShortDateString(),
                    TimeZoneInfo.ConvertTime(max, tz).ToShortDateString()));

                // Header
                writer.PageEvent = new Header(
                    string.Format("REPORT: PUNCHES BY USER {0} thru {1}",
                        TimeZoneInfo.ConvertTime(min, tz).ToShortDateString(),
                        TimeZoneInfo.ConvertTime(max, tz).ToShortDateString()),
                    TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz));
                
                // Open the document to enable you to write to the document
                document.Open();
                
                // Build table of punches
                PdfPTable table = new PdfPTable(8);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 10, 7, 10, 7, 20, 20, 20, 6 });

                var users = db.Users.Where(u => userIds.Contains(u.Id)).OrderBy(u => u.Name).ToList();
                foreach (var user in users)
                {
                    // User Name and Spacer
                    var nameCell = new PdfPCell(new Phrase(string.Format("User {0}", user.Name), fontH1))
                    {
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Colspan = 8,
                        Padding = 5,
                        PaddingTop = 8,
                        PaddingBottom = 8,
                        BackgroundColor = BaseColor.BLACK,
                        UseAscender = true
                    };
                    table.AddCell(nameCell);
                    var nameSpacerCell = new PdfPCell(new Phrase(" "))
                    {
                        Colspan = 8,
                        Padding = 0,
                        PaddingBottom = 8,
                        BorderWidthLeft = 0,
                        BorderWidthRight = 0
                    };
                    table.AddCell(nameSpacerCell);

                    var punches = db.Punches
                        .Where(p => p.OutAt.HasValue)
                        .Where(p => p.InAt >= min && p.OutAt <= max)
                        .Where(p => p.UserId == user.Id)
                        .OrderBy(p => p.InAt)
                        .ToList();
                    
                    // Add a message for no punches
                    if (punches.Count() == 0)
                    {
                        var noneCell = new PdfPCell(new Phrase(
                            string.Format("There are no punches for {0} between {1} and {2}",
                                user.Name,
                                TimeZoneInfo.ConvertTime(min, tz).ToShortDateString(),
                                TimeZoneInfo.ConvertTime(max, tz).ToShortDateString()),
                            fontP))
                        {
                            Colspan = 8,
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(noneCell);
                    }
                    
                    // Loop each date and print each punch
                    var dates = punches
                        .GroupBy(p => TimeZoneInfo.ConvertTime(p.InAt, tz).Date)
                        .Select(g => new {
                            Date = g.Key
                        })
                        .ToList();
                    foreach (var date in dates)
                    {
                        // Day
                        var dayCell = new PdfPCell(new Phrase(date.Date.ToString("D"), fontH1))
                        {
                            Colspan = 8,
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            BackgroundColor = BaseColor.BLACK,
                            UseAscender = true
                        };
                        table.AddCell(dayCell);

                        // Column Headers
                        var inHeaderCell = new PdfPCell(new Phrase("In", fontH3))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(inHeaderCell);

                        var sourceInHeaderCell = new PdfPCell(new Phrase("Source", fontH3))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(sourceInHeaderCell);

                        var outHeaderCell = new PdfPCell(new Phrase("Out", fontH3))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(outHeaderCell);

                        var sourceOutHeaderCell = new PdfPCell(new Phrase("Source", fontH3))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(sourceOutHeaderCell);

                        var taskHeaderCell = new PdfPCell(new Phrase("Task", fontH3))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(taskHeaderCell);

                        var jobHeaderCell = new PdfPCell(new Phrase("Job", fontH3))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(jobHeaderCell);

                        var customerHeaderCell = new PdfPCell(new Phrase("Customer", fontH3))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(customerHeaderCell);

                        var totalHeaderCell = new PdfPCell(new Phrase("Total", fontH3))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(totalHeaderCell);

                        // Punches for this Day
                        var punchesForDay = punches
                            .Where(p => TimeZoneInfo.ConvertTime(p.InAt, tz).Date == date.Date)
                            .ToList();
                        foreach (var punch in punchesForDay)
                        {
                            // In At
                            var inCell = new PdfPCell(new Phrase(TimeZoneInfo.ConvertTime(punch.InAt, tz).ToString("h:mmtt").ToLower(), fontP))
                            {
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                Padding = 5,
                                UseAscender = true
                            };
                            table.AddCell(inCell);

                            // In At Source
                            var sourceInCell = new PdfPCell(new Phrase(punch.SourceForInAt[0].ToString(), fontP))
                            {
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                Padding = 5,
                                UseAscender = true
                            };
                            table.AddCell(sourceInCell);

                            // Out At
                            var outCell = new PdfPCell(new Phrase(TimeZoneInfo.ConvertTime(punch.OutAt.Value, tz).ToString("h:mmtt").ToLower(), fontP))
                            {
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                Padding = 5,
                                UseAscender = true
                            };
                            table.AddCell(outCell);

                            // Out At Source
                            var sourceOutCell = new PdfPCell(new Phrase(punch.SourceForOutAt[0].ToString(), fontP))
                            {
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                Padding = 5,
                                UseAscender = true
                            };
                            table.AddCell(sourceOutCell);

                            // Task
                            var taskCell = new PdfPCell(new Phrase(string.Format("{0} - {1}", punch.Task.Number, punch.Task.Name), fontP))
                            {
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                Padding = 5,
                                UseAscender = true
                            };
                            table.AddCell(taskCell);

                            // Job
                            var jobCell = new PdfPCell(new Phrase(string.Format("{0} - {1}", punch.Task.Job.Number, punch.Task.Job.Name), fontP))
                            {
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                Padding = 5,
                                UseAscender = true
                            };
                            table.AddCell(jobCell);

                            // Customer
                            var customerCell = new PdfPCell(new Phrase(string.Format("{0} - {1}", punch.Task.Job.Customer.Number, punch.Task.Job.Customer.Name), fontP))
                            {
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                Padding = 5,
                                UseAscender = true
                            };
                            table.AddCell(customerCell);

                            // Total
                            var total = Math.Round((punch.OutAt.Value - punch.InAt).TotalMinutes / 60, 2).ToString("0.00");
                            var totalCell = new PdfPCell(new Phrase(total.ToString(), fontP))
                            {
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                Padding = 5,
                                UseAscender = true
                            };
                            table.AddCell(totalCell);
                        }

                        // Daily Total and Spacer
                        double dailyTotalMinutes = 0;
                        foreach (var punch in punchesForDay)
                        {
                            dailyTotalMinutes += (punch.OutAt.Value - punch.InAt).TotalMinutes;
                        }
                        var dailyTotal = Math.Round(dailyTotalMinutes / 60, 2).ToString("0.00");
                        var dailyTotalHeaderCell = new PdfPCell(new Phrase("Daily Total", fontH3))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Colspan = 7,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(dailyTotalHeaderCell);
                        var dailyTotalValueCell = new PdfPCell(new Phrase(dailyTotal.ToString(), fontH3))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(dailyTotalValueCell);
                        var dailyTotalSpacerCell = new PdfPCell(new Phrase(" "))
                        {
                            Colspan = 8,
                            Padding = 0,
                            PaddingBottom = 4,
                            BorderWidthLeft = 0,
                            BorderWidthRight = 0
                        };
                        table.AddCell(dailyTotalSpacerCell);
                    }

                    // User Total
                    double userTotalMinutes = 0;
                    foreach (var punch in punches)
                    {
                        userTotalMinutes += (punch.OutAt.Value - punch.InAt).TotalMinutes;
                    }
                    var userTotal = Math.Round(userTotalMinutes / 60, 2).ToString("0.00");
                    var userTotalHeaderCell = new PdfPCell(new Phrase(string.Format("Total for User {0}", user.Name), fontH3))
                    {
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        HorizontalAlignment = Element.ALIGN_RIGHT,
                        Colspan = 7,
                        Padding = 5
                    };
                    table.AddCell(userTotalHeaderCell);
                    var userTotalValueCell = new PdfPCell(new Phrase(userTotal.ToString(), fontH3))
                    {
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 5
                    };
                    table.AddCell(userTotalValueCell);

                    // Page break
                    if (users.Last() == user)
                    {
                        document.NewPage();
                    }
                }
                
                // Add a message for no users
                if (users.Count() == 0)
                {
                    document.Add(new Phrase(
                        string.Format("There are no users with punches between {0} and {1}",
                            TimeZoneInfo.ConvertTime(min, tz).ToShortDateString(),
                            TimeZoneInfo.ConvertTime(max, tz).ToShortDateString()),
                        fontP));
                }

                document.Add(table);

                // Make sure data has been written
                writer.Flush();

                // Close the document
                document.Close();
            }

            buffer = output.GetBuffer();

            // Page count must be added later due to nuances in iText
            AddPageNumbers(buffer);

            return buffer;
        }

        public byte[] PunchesByJobAndTaskAsPdf(int[] userIds, int[] jobIds, DateTime min, DateTime max, User currentUser)
        {
            var buffer = new byte[0];
            var output = new MemoryStream();
            var organization = db.Organizations.Find(currentUser.OrganizationId);
            var tz = TimeZoneInfo.FindSystemTimeZoneById(organization.TimeZone);

            // Create an instance of document which represents the PDF document itself
            using (var document = new Document(PageSize.LETTER.Rotate(), 30, 30, 60, 60))
            {
                var writer = PdfWriter.GetInstance(document, output);
                pageWidth = document.PageSize.Width;

                // Add meta information to the document
                document.AddAuthor(currentUser.Name);
                document.AddCreator("BRIZBEE");
                document.AddTitle(string.Format(
                    "Punches by Job and Task {0} thru {1}.pdf",
                    TimeZoneInfo.ConvertTime(min, tz).ToShortDateString(),
                    TimeZoneInfo.ConvertTime(max, tz).ToShortDateString()));

                // Header
                writer.PageEvent = new Header(
                    string.Format("REPORT: PUNCHES BY JOB AND TASK {0} thru {1}",
                        TimeZoneInfo.ConvertTime(min, tz).ToShortDateString(),
                        TimeZoneInfo.ConvertTime(max, tz).ToShortDateString()),
                    TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz));

                // Open the document to enable you to write to the document
                document.Open();

                // Build table of punches
                PdfPTable table = new PdfPTable(7);
                table.WidthPercentage = 100;

                // Select all job ids to generate report if no jobs are filtered
                int[] filteredJobIds;
                if (jobIds.Count() == 0)
                {
                    var customerIds = db.Customers
                        .Where(c => c.OrganizationId == currentUser.OrganizationId)
                        .Select(c => c.Id);
                    filteredJobIds = db.Jobs
                        .Where(j => customerIds.Contains(j.CustomerId))
                        .Select(j => j.Id)
                        .ToArray();
                }
                else
                {
                    filteredJobIds = jobIds;
                }

                var punches = db.Punches
                    .Include("Task")
                    .Include("Task.Job")
                    .Where(p => p.OutAt.HasValue)
                    .Where(p => p.InAt >= min && p.OutAt <= max)
                    .Where(p => filteredJobIds.Contains(p.Task.JobId))
                    .ToList();
                
                var groupedJobIds = punches
                    .GroupBy(p => p.Task.JobId)
                    .Select(g => g.Key)
                    .ToList();

                foreach (var jobId in groupedJobIds)
                {
                    var job = db.Jobs.Find(jobId);
                    var groupedTaskIds = punches
                        .GroupBy(p => p.TaskId)
                        .Select(g => g.Key)
                        .ToList();

                    // Job Name
                    var jobCell = new PdfPCell(new Paragraph(string.Format("Job {0} - {1} for Customer {2} - {3}", job.Number, job.Name, job.Customer.Number, job.Customer.Name), fontH1));
                    jobCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    jobCell.Colspan = 7;
                    jobCell.Padding = 5;
                    jobCell.PaddingBottom = 15;
                    jobCell.BackgroundColor = BaseColor.BLACK;
                    table.AddCell(jobCell);
                    
                    // Loop each date and print each punch
                    var dates = punches
                        .GroupBy(p => TimeZoneInfo.ConvertTime(p.InAt, tz).Date)
                        .Select(g => new {
                            Date = g.Key
                        })
                        .ToList();
                    foreach (var date in dates)
                    {
                        // Day
                        var dayCell = new PdfPCell(new Paragraph(date.Date.ToString("D"), fontH1));
                        dayCell.Colspan = 7;
                        dayCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        dayCell.Padding = 5;
                        dayCell.BackgroundColor = BaseColor.BLACK;
                        table.AddCell(dayCell);

                        // Column Headers
                        var inHeaderCell = new PdfPCell(new Paragraph("In", fontH3));
                        inHeaderCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        inHeaderCell.Padding = 5;
                        table.AddCell(inHeaderCell);

                        var sourceInHeaderCell = new PdfPCell(new Paragraph("Source", fontH3));
                        sourceInHeaderCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        sourceInHeaderCell.Padding = 5;
                        table.AddCell(sourceInHeaderCell);

                        var outHeaderCell = new PdfPCell(new Paragraph("Out", fontH3));
                        outHeaderCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        outHeaderCell.Padding = 5;
                        table.AddCell(outHeaderCell);

                        var sourceOutHeaderCell = new PdfPCell(new Paragraph("Source", fontH3));
                        sourceOutHeaderCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        sourceOutHeaderCell.Padding = 5;
                        table.AddCell(sourceOutHeaderCell);

                        var userHeaderCell = new PdfPCell(new Paragraph("User", fontH3));
                        userHeaderCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        userHeaderCell.Padding = 5;
                        table.AddCell(userHeaderCell);

                        var taskHeaderCell = new PdfPCell(new Paragraph("Task", fontH3));
                        taskHeaderCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        taskHeaderCell.Padding = 5;
                        table.AddCell(taskHeaderCell);

                        var totalHeaderCell = new PdfPCell(new Paragraph("Total", fontH3));
                        totalHeaderCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        totalHeaderCell.Padding = 5;
                        table.AddCell(totalHeaderCell);

                        // Punches for this job
                        var punchesForJobAndDay = punches
                            .Where(p => TimeZoneInfo.ConvertTime(p.InAt, tz).Date == date.Date)
                            .Where(p => groupedTaskIds.Contains(p.TaskId))
                            .ToList();
                        foreach (var punch in punchesForJobAndDay)
                        {
                            // In At
                            var inCell = new PdfPCell(new Paragraph(string.Format("{0} {1}", TimeZoneInfo.ConvertTime(punch.InAt, tz).ToString("MMM dd, yyyy"), TimeZoneInfo.ConvertTime(punch.InAt, tz).ToString("h:mmtt").ToLower()), fontP));
                            inCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                            inCell.Padding = 5;
                            table.AddCell(inCell);

                            // In At Source
                            var sourceInCell = new PdfPCell(new Paragraph(punch.SourceForInAt[0].ToString(), fontP));
                            sourceInCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                            sourceInCell.Padding = 5;
                            table.AddCell(sourceInCell);

                            // Out At
                            var outCell = new PdfPCell(new Paragraph(string.Format("{0} {1}", TimeZoneInfo.ConvertTime(punch.OutAt.Value, tz).ToString("MMM dd, yyyy"), TimeZoneInfo.ConvertTime(punch.OutAt.Value, tz).ToString("h:mmtt").ToLower()), fontP));
                            outCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                            outCell.Padding = 5;
                            table.AddCell(outCell);

                            // Out At Source
                            var sourceOutCell = new PdfPCell(new Paragraph(punch.SourceForOutAt[0].ToString(), fontP));
                            sourceOutCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                            sourceOutCell.Padding = 5;
                            table.AddCell(sourceOutCell);

                            // User
                            var userCell = new PdfPCell(new Paragraph(punch.User.Name, fontP));
                            userCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                            userCell.Padding = 5;
                            table.AddCell(userCell);

                            // Task
                            var taskCell = new PdfPCell(new Paragraph(string.Format("{0} - {1}", punch.Task.Number, punch.Task.Name), fontP));
                            taskCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                            taskCell.Padding = 5;
                            table.AddCell(taskCell);

                            // Total
                            var total = Math.Round((punch.OutAt.Value - punch.InAt).TotalMinutes / 60, 2).ToString("0.00");
                            var totalCell = new PdfPCell(new Paragraph(total.ToString(), fontP));
                            totalCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                            totalCell.Padding = 5;
                            table.AddCell(totalCell);
                        }

                        // Daily Total
                        double dailyTotalMinutes = 0;
                        foreach (var punch in punchesForJobAndDay)
                        {
                            dailyTotalMinutes += (punch.OutAt.Value - punch.InAt).TotalMinutes;
                        }
                        var dailyTotal = Math.Round(dailyTotalMinutes / 60, 2).ToString("0.00");
                        var dailyTotalHeaderCell = new PdfPCell(new Paragraph("Daily Total", fontH3));
                        dailyTotalHeaderCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        dailyTotalHeaderCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                        dailyTotalHeaderCell.Colspan = 6;
                        dailyTotalHeaderCell.Padding = 5;
                        table.AddCell(dailyTotalHeaderCell);
                        var dailyTotalValueCell = new PdfPCell(new Paragraph(dailyTotal.ToString(), fontH3));
                        dailyTotalValueCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        dailyTotalValueCell.Padding = 5;
                        table.AddCell(dailyTotalValueCell);
                    }
                }

                // Add a message for no jobs
                if (filteredJobIds.Count() == 0)
                {
                    document.Add(new Paragraph(
                        string.Format("There are no jobs with punches between {0} and {1}",
                            TimeZoneInfo.ConvertTime(min, tz).ToShortDateString(),
                            TimeZoneInfo.ConvertTime(max, tz).ToShortDateString()),
                        fontP));
                }

                document.Add(table);

                // Make sure data has been written
                writer.Flush();

                // Close the document
                document.Close();
            }

            buffer = output.GetBuffer();

            // Page count must be added later due to nuances in iText
            AddPageNumbers(buffer);

            return buffer;
        }

        public byte[] PunchesByDayAsPdf(int[] userIds, int[] jobIds, DateTime min, DateTime max, User currentUser)
        {
            var buffer = new byte[0];
            var output = new MemoryStream();
            var organization = db.Organizations.Find(currentUser.OrganizationId);
            var tz = TimeZoneInfo.FindSystemTimeZoneById(organization.TimeZone);

            // Create an instance of document which represents the PDF document itself
            using (var document = new Document(PageSize.LETTER.Rotate(), 30, 30, 60, 60))
            {
                var writer = PdfWriter.GetInstance(document, output);
                pageWidth = document.PageSize.Width;

                // Add meta information to the document
                document.AddAuthor(currentUser.Name);
                document.AddCreator("BRIZBEE");
                document.AddTitle(string.Format(
                    "Punches by Day {0} thru {1}.pdf",
                    TimeZoneInfo.ConvertTime(min, tz).ToShortDateString(),
                    TimeZoneInfo.ConvertTime(max, tz).ToShortDateString()));

                // Header
                writer.PageEvent = new Header(
                    string.Format("REPORT: PUNCHES BY DAY {0} thru {1}",
                        TimeZoneInfo.ConvertTime(min, tz).ToShortDateString(),
                        TimeZoneInfo.ConvertTime(max, tz).ToShortDateString()),
                    TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz));

                // Open the document to enable you to write to the document
                document.Open();

                // Build table of punches
                PdfPTable table = new PdfPTable(9);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 6, 6, 6, 6, 17, 17, 17, 17, 8 });

                // Select all job ids to generate report if no jobs are filtered
                int[] filteredJobIds;
                if (jobIds.Count() == 0)
                {
                    var customerIds = db.Customers
                        .Where(c => c.OrganizationId == currentUser.OrganizationId)
                        .Select(c => c.Id);
                    filteredJobIds = db.Jobs
                        .Where(j => customerIds.Contains(j.CustomerId))
                        .Select(j => j.Id)
                        .ToArray();
                }
                else
                {
                    filteredJobIds = jobIds;
                }

                var punches = db.Punches
                    .Include("Task")
                    .Include("Task.Job")
                    .Where(p => p.OutAt.HasValue)
                    .Where(p => p.InAt >= min && p.OutAt <= max)
                    .Where(p => filteredJobIds.Contains(p.Task.JobId))
                    .ToList();
                
                // Loop each date and print each punch
                var dates = punches
                    .GroupBy(p => TimeZoneInfo.ConvertTime(p.InAt, tz).Date)
                    .Select(g => new {
                        Date = g.Key
                    })
                    .ToList();
                foreach (var date in dates)
                {
                    // Day
                    var dayCell = new PdfPCell(new Paragraph(date.Date.ToString("D"), fontH1));
                    dayCell.Colspan = 9;
                    dayCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    dayCell.Padding = 5;
                    dayCell.PaddingBottom = 15;
                    dayCell.BackgroundColor = BaseColor.BLACK;
                    table.AddCell(dayCell);

                    // Column Headers
                    var inHeaderCell = new PdfPCell(new Paragraph("In", fontH3));
                    inHeaderCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    inHeaderCell.Padding = 5;
                    table.AddCell(inHeaderCell);

                    var sourceInHeaderCell = new PdfPCell(new Paragraph("Source", fontH3));
                    sourceInHeaderCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    sourceInHeaderCell.Padding = 5;
                    table.AddCell(sourceInHeaderCell);

                    var outHeaderCell = new PdfPCell(new Paragraph("Out", fontH3));
                    outHeaderCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    outHeaderCell.Padding = 5;
                    table.AddCell(outHeaderCell);

                    var sourceOutHeaderCell = new PdfPCell(new Paragraph("Source", fontH3));
                    sourceOutHeaderCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    sourceOutHeaderCell.Padding = 5;
                    table.AddCell(sourceOutHeaderCell);

                    var userHeaderCell = new PdfPCell(new Paragraph("User", fontH3));
                    userHeaderCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    userHeaderCell.Padding = 5;
                    table.AddCell(userHeaderCell);

                    var taskHeaderCell = new PdfPCell(new Paragraph("Task", fontH3));
                    taskHeaderCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    taskHeaderCell.Padding = 5;
                    table.AddCell(taskHeaderCell);

                    var jobHeaderCell = new PdfPCell(new Paragraph("Job", fontH3));
                    jobHeaderCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    jobHeaderCell.Padding = 5;
                    table.AddCell(jobHeaderCell);

                    var customerHeaderCell = new PdfPCell(new Paragraph("Customer", fontH3));
                    customerHeaderCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    customerHeaderCell.Padding = 5;
                    table.AddCell(customerHeaderCell);

                    var totalHeaderCell = new PdfPCell(new Paragraph("Total", fontH3));
                    totalHeaderCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    totalHeaderCell.Padding = 5;
                    table.AddCell(totalHeaderCell);

                    // Punches for this Day
                    var punchesForDay = punches
                        .Where(p => TimeZoneInfo.ConvertTime(p.InAt, tz).Date == date.Date)
                        .ToList();
                    foreach (var punch in punchesForDay)
                    {
                        // In At
                        var inCell = new PdfPCell(new Paragraph(TimeZoneInfo.ConvertTime(punch.InAt, tz).ToString("h:mmtt").ToLower(), fontP));
                        inCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        inCell.Padding = 5;
                        table.AddCell(inCell);

                        // In At Source
                        var sourceInCell = new PdfPCell(new Paragraph(punch.SourceForInAt[0].ToString(), fontP));
                        sourceInCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        sourceInCell.Padding = 5;
                        table.AddCell(sourceInCell);

                        // Out At
                        var outCell = new PdfPCell(new Paragraph(TimeZoneInfo.ConvertTime(punch.OutAt.Value, tz).ToString("h:mmtt").ToLower(), fontP));
                        outCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        outCell.Padding = 5;
                        table.AddCell(outCell);

                        // Out At Source
                        var sourceOutCell = new PdfPCell(new Paragraph(punch.SourceForOutAt[0].ToString(), fontP));
                        sourceOutCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        sourceOutCell.Padding = 5;
                        table.AddCell(sourceOutCell);

                        // User
                        var userCell = new PdfPCell(new Paragraph(punch.User.Name, fontP));
                        userCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        userCell.Padding = 5;
                        table.AddCell(userCell);

                        // Task
                        var taskCell = new PdfPCell(new Paragraph(string.Format("{0} - {1}", punch.Task.Number, punch.Task.Name), fontP));
                        taskCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        taskCell.Padding = 5;
                        table.AddCell(taskCell);

                        // Job
                        var jobCell = new PdfPCell(new Paragraph(string.Format("{0} - {1}", punch.Task.Job.Number, punch.Task.Job.Name), fontP));
                        jobCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        jobCell.Padding = 5;
                        table.AddCell(jobCell);

                        // Customer
                        var customerCell = new PdfPCell(new Paragraph(string.Format("{0} - {1}", punch.Task.Job.Customer.Number, punch.Task.Job.Customer.Name), fontP));
                        customerCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        customerCell.Padding = 5;
                        table.AddCell(customerCell);

                        // Total
                        var total = Math.Round((punch.OutAt.Value - punch.InAt).TotalMinutes / 60, 2).ToString("0.00");
                        var totalCell = new PdfPCell(new Paragraph(total.ToString(), fontP));
                        totalCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        totalCell.Padding = 5;
                        table.AddCell(totalCell);
                    }

                    // Daily Total
                    double dailyTotalMinutes = 0;
                    foreach (var punch in punchesForDay)
                    {
                        dailyTotalMinutes += (punch.OutAt.Value - punch.InAt).TotalMinutes;
                    }
                    var dailyTotal = Math.Round(dailyTotalMinutes / 60, 2).ToString("0.00");
                    var dailyTotalHeaderCell = new PdfPCell(new Paragraph("Daily Total", fontH3));
                    dailyTotalHeaderCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    dailyTotalHeaderCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    dailyTotalHeaderCell.Colspan = 8;
                    dailyTotalHeaderCell.Padding = 5;
                    table.AddCell(dailyTotalHeaderCell);
                    var dailyTotalValueCell = new PdfPCell(new Paragraph(dailyTotal.ToString(), fontH3));
                    dailyTotalValueCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    dailyTotalValueCell.Padding = 5;
                    table.AddCell(dailyTotalValueCell);
                }
                
                // Add a message for no punches
                if (punches.Count() == 0)
                {
                    document.Add(new Paragraph(
                        string.Format("There are no punches between {0} and {1}",
                            TimeZoneInfo.ConvertTime(min, tz).ToShortDateString(),
                            TimeZoneInfo.ConvertTime(max, tz).ToShortDateString()),
                        fontP));
                }

                document.Add(table);

                // Make sure data has been written
                writer.Flush();

                // Close the document
                document.Close();
            }

            buffer = output.GetBuffer();

            // Page count must be added later due to nuances in iText
            AddPageNumbers(buffer);

            return buffer;
        }

        private void AddPageNumbers(byte[] buffer)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                PdfReader reader = new PdfReader(buffer);
                using (PdfStamper stamper = new PdfStamper(reader, stream))
                {
                    int pages = reader.NumberOfPages;
                    for (int i = 1; i <= pages; i++)
                    {
                        var text = string.Format("Page {0} of {1}", i, pages);
                        ColumnText.ShowTextAligned(stamper.GetUnderContent(i), Element.ALIGN_RIGHT, new Phrase(text, fontFooter), pageWidth - 30, 30f, 0);
                    }
                }
                buffer = stream.ToArray();
            }
        }
    }
    
    public class Header : PdfPageEventHelper
    {
        // This is the contentbyte object of the writer
        PdfContentByte cb;

        // we will put the final number of pages in a template
        PdfTemplate headerTemplate, footerTemplate;

        // this is the BaseFont we are going to use for the header / footer
        BaseFont bf = null;
        
        public string Title { get; set; }
        public DateTime PrintTime { get; set; }

        public Header(string title, DateTime printTime)
        {
            Title = title;
            PrintTime = printTime;
        }

        public override void OnOpenDocument(PdfWriter writer, Document document)
        {
            try
            {
                bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                cb = writer.DirectContent;

                var width = document.PageSize.Width - document.LeftMargin - document.RightMargin;

                headerTemplate = cb.CreateTemplate(width, 30);
                footerTemplate = cb.CreateTemplate(width, 30);
            }
            catch (DocumentException)
            {
            }
            catch (IOException)
            {
            }
        }

        public override void OnEndPage(PdfWriter writer, Document document)
        {
            base.OnEndPage(writer, document);

            Font baseFontNormal = new Font(Font.FontFamily.HELVETICA, 9, Font.NORMAL, BaseColor.BLACK);
            Font baseFontBig = new Font(Font.FontFamily.HELVETICA, 9, Font.BOLD, BaseColor.BLACK);
            
            PdfPTable tableHeader = new PdfPTable(2);
            
            PdfPCell headerCell1 = new PdfPCell(new Paragraph(Title, baseFontNormal));
            PdfPCell headerCell2 = new PdfPCell(new Paragraph(string.Format("Printed {0}", PrintTime.ToString("F")), baseFontNormal));
            
            // Set cell styling
            headerCell1.HorizontalAlignment = Element.ALIGN_LEFT;
            headerCell2.HorizontalAlignment = Element.ALIGN_RIGHT;
            headerCell1.VerticalAlignment = Element.ALIGN_MIDDLE;
            headerCell2.VerticalAlignment = Element.ALIGN_MIDDLE;
            headerCell1.Border = 0;
            headerCell2.Border = 0;

            // Add cells to table
            tableHeader.AddCell(headerCell1);
            tableHeader.AddCell(headerCell2);
            
            // Table is full width
            tableHeader.TotalWidth = document.PageSize.Width - document.LeftMargin - document.RightMargin;

            //call WriteSelectedRows of PdfTable. This writes rows from PdfWriter in PdfTable
            //first param is start row. -1 indicates there is no end row and all the rows to be included to write
            //Third and fourth param is x and y position to start writing
            //table.WriteSelectedRows(0, -1, 40, document.PageSize.Height - 30, writer.DirectContent);
            tableHeader.WriteSelectedRows(0, -1, document.LeftMargin, document.PageSize.Height - 30, writer.DirectContent);



            PdfPTable tableFooter = new PdfPTable(1);

            string contentPath = HostingEnvironment.MapPath("~/Content");
            Image logo = Image.GetInstance(contentPath + "/logo.png");
            logo.ScaleAbsolute(61f, 10f);
            PdfPCell footerCell1 = new PdfPCell(logo);

            // Set cell styling
            footerCell1.HorizontalAlignment = Element.ALIGN_LEFT;
            footerCell1.VerticalAlignment = Element.ALIGN_MIDDLE;
            footerCell1.Border = 0;

            // Add cells to table
            tableFooter.AddCell(footerCell1);

            // Table is full width
            tableFooter.TotalWidth = document.PageSize.Width - document.LeftMargin - document.RightMargin;

            tableFooter.WriteSelectedRows(0, -1, document.LeftMargin, document.PageSize.GetBottom(38), writer.DirectContent);

            //Move the pointer and draw line to separate header section from rest of page
            cb.MoveTo(document.LeftMargin, document.PageSize.Height - 50);
            cb.LineTo(document.PageSize.Width - document.RightMargin, document.PageSize.Height - 50);
            cb.Stroke();

            //Move the pointer and draw line to separate footer section from rest of page
            cb.MoveTo(document.LeftMargin, document.PageSize.GetBottom(50));
            cb.LineTo(document.PageSize.Width - document.RightMargin, document.PageSize.GetBottom(50));
            cb.Stroke();
        }
    }
}