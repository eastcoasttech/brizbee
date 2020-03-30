using Brizbee.Common.Database;
using Brizbee.Common.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web.Hosting;

namespace Brizbee.Web.Services
{
    public class ReportBuilder
    {
        private SqlContext db = new SqlContext();
        private Font fontH1 = new Font(Font.FontFamily.HELVETICA, 9, Font.BOLD, BaseColor.WHITE);
        private Font fontH2 = new Font(Font.FontFamily.HELVETICA, 9, Font.BOLDITALIC);
        private Font fontH3 = new Font(Font.FontFamily.HELVETICA, 8, Font.BOLD);
        private Font fontH4 = new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD);
        private Font fontP = new Font(Font.FontFamily.HELVETICA, 8, Font.NORMAL);
        private Font fontFooter = new Font(Font.FontFamily.HELVETICA, 9, Font.NORMAL);
        private float pageWidth = 0f;

        public byte[] PunchesByUserAsPdf(string userScope, int[] userIds, string jobScope, int[] jobIds, DateTime min, DateTime max, string commitStatus, User currentUser)
        {
            var buffer = new byte[0];
            var output = new MemoryStream();
            var tz = DateTimeZoneProviders.Tzdb.GetZoneOrNull(currentUser.TimeZone);
            var nowInstant = SystemClock.Instance.GetCurrentInstant();
            var nowLocal = nowInstant.InZone(tz);
            var nowDateTime = nowLocal.LocalDateTime.ToDateTimeUnspecified();

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
                    min.ToShortDateString(),
                    max.ToShortDateString()));

                // Header
                writer.PageEvent = new Header(
                    string.Format("REPORT: PUNCHES BY USER {0} thru {1}",
                        min.ToShortDateString(),
                        max.ToShortDateString()),
                    nowDateTime);
                
                // Open the document to enable you to write to the document
                document.Open();
                
                // Build table of punches
                PdfPTable table = new PdfPTable(9);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 7, 4, 7, 4, 22, 22, 22, 6, 6 });
                
                // Get the users depending on the filtered scope
                List<User> users;
                if (userScope == "specific")
                {
                    users = db.Users
                        .Where(u => userIds.Contains(u.Id))
                        .OrderBy(u => u.Name)
                        .ToList();
                }
                else
                {
                    users = db.Users
                        .Where(u => u.OrganizationId == currentUser.OrganizationId)
                        .OrderBy(u => u.Name)
                        .ToList();
                }
                foreach (var user in users)
                {
                    // User Name and Spacer
                    var nameCell = new PdfPCell(new Phrase(string.Format("User {0}", user.Name), fontH1))
                    {
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Colspan = 9,
                        Padding = 5,
                        PaddingTop = 8,
                        PaddingBottom = 8,
                        BackgroundColor = BaseColor.BLACK,
                        UseAscender = true
                    };
                    table.AddCell(nameCell);
                    var nameSpacerCell = new PdfPCell(new Phrase(" "))
                    {
                        Colspan = 9,
                        Padding = 0,
                        PaddingBottom = 8,
                        BorderWidthLeft = 0,
                        BorderWidthRight = 0,
                        UseAscender = true
                    };
                    table.AddCell(nameSpacerCell);

                    // Get all the punches for the user
                    // either for any job or a specific job
                    IQueryable<Punch> punchesQueryable = db.Punches
                        .Include("Task")
                        .Where(p => p.OutAt.HasValue == true)
                        .Where(p => p.InAt >= min && p.OutAt.Value <= max)
                        .Where(p => p.UserId == user.Id);
                    
                    // Filter by job
                    if (jobScope == "specific")
                    {
                        punchesQueryable = punchesQueryable
                            .Where(p => jobIds.Contains(p.Task.JobId));
                    }

                    // Filter by commit
                    if (commitStatus == "only")
                    {
                        punchesQueryable = punchesQueryable.Where(p => p.CommitId != null);
                    }
                    else if (commitStatus == "uncommitted")
                    {
                        punchesQueryable = punchesQueryable.Where(p => p.CommitId == null);
                    }

                    var punches = punchesQueryable
                        .OrderBy(p => p.InAt)
                        .ToList();

                    if (punches.Count() == 0)
                    {
                        // Add a message if there are no punches
                        var noneCell = new PdfPCell(new Phrase(
                            string.Format("There are no punches for {0} between {1} and {2}",
                                user.Name,
                                min.ToShortDateString(),
                                max.ToShortDateString()),
                            fontP))
                        {
                            Colspan = 9,
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(noneCell);
                    }
                    
                    // Loop each date and print each punch
                    var dates = punches
                        .GroupBy(p => p.InAt.Date)
                        .Select(g => new {
                            Date = g.Key
                        })
                        .ToList();
                    foreach (var date in dates)
                    {
                        // Day
                        var dayCell = new PdfPCell(new Phrase(date.Date.ToString("D"), fontH1))
                        {
                            Colspan = 9,
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

                        var sourceInHeaderCell = new PdfPCell(new Phrase("Src", fontH3))
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

                        var sourceOutHeaderCell = new PdfPCell(new Phrase("Src", fontH3))
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

                        var committedHeaderCell = new PdfPCell(new Phrase("Cmted", fontH3))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(committedHeaderCell);

                        var totalHeaderCell = new PdfPCell(new Phrase("Total", fontH3))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(totalHeaderCell);

                        // Punches for this Day
                        var punchesForDay = punches
                            .Where(p => p.InAt.Date == date.Date)
                            .ToList();
                        foreach (var punch in punchesForDay)
                        {
                            // In At
                            var inCell = new PdfPCell(new Phrase(punch.InAt.ToString("h:mmtt").ToLower(), fontP))
                            {
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                Padding = 5,
                                UseAscender = true
                            };
                            table.AddCell(inCell);

                            // In At Source
                            var sourceForInAt = string.IsNullOrEmpty(punch.SourceForInAt) ? "" : punch.SourceForInAt[0].ToString();
                            var sourceInCell = new PdfPCell(new Phrase(sourceForInAt, fontP))
                            {
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                Padding = 5,
                                UseAscender = true
                            };
                            table.AddCell(sourceInCell);

                            // Out At
                            var outCell = new PdfPCell(new Phrase(punch.OutAt.Value.ToString("h:mmtt").ToLower(), fontP))
                            {
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                Padding = 5,
                                UseAscender = true
                            };
                            table.AddCell(outCell);

                            // Out At Source
                            var sourceForOutAt = string.IsNullOrEmpty(punch.SourceForOutAt) ? "" : punch.SourceForOutAt[0].ToString();
                            var sourceOutCell = new PdfPCell(new Phrase(sourceForOutAt, fontP))
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

                            // Committed
                            var committed = punch.CommitId.HasValue ? "X" : "";
                            var committedCell = new PdfPCell(new Phrase(committed, fontP))
                            {
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                HorizontalAlignment = Element.ALIGN_CENTER,
                                Padding = 5,
                                UseAscender = true
                            };
                            table.AddCell(committedCell);

                            // Total
                            var total = Math.Round((punch.OutAt.Value - punch.InAt).TotalMinutes / 60, 2).ToString("0.00");
                            var totalCell = new PdfPCell(new Phrase(total.ToString(), fontP))
                            {
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                HorizontalAlignment = Element.ALIGN_RIGHT,
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
                            Colspan = 8,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(dailyTotalHeaderCell);
                        var dailyTotalValueCell = new PdfPCell(new Phrase(dailyTotal.ToString(), fontH3))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(dailyTotalValueCell);
                        var dailyTotalSpacerCell = new PdfPCell(new Phrase(" "))
                        {
                            Colspan = 9,
                            Padding = 0,
                            PaddingBottom = 4,
                            BorderWidthLeft = 0,
                            BorderWidthRight = 0,
                            UseAscender = true
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
                        Colspan = 8,
                        Padding = 5,
                        UseAscender = true
                    };
                    table.AddCell(userTotalHeaderCell);
                    var userTotalValueCell = new PdfPCell(new Phrase(userTotal.ToString(), fontH3))
                    {
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        HorizontalAlignment = Element.ALIGN_RIGHT,
                        Padding = 5,
                        UseAscender = true
                    };
                    table.AddCell(userTotalValueCell);

                    var totalSpacerCell = new PdfPCell(new Phrase(" "))
                    {
                        Colspan = 9,
                        Padding = 0,
                        PaddingBottom = 8,
                        BorderWidthLeft = 0,
                        BorderWidthRight = 0,
                        UseAscender = true
                    };
                    table.AddCell(totalSpacerCell);

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
                            min.ToShortDateString(),
                            max.ToShortDateString()),
                        fontP));
                }

                document.Add(table);

                // Make sure data has been written
                writer.Flush();

                // Close the document
                document.Close();
            }

            buffer = output.ToArray();

            // Page count must be added later due to nuances in iText
            AddPageNumbers(buffer);

            return buffer;
        }

        public byte[] PunchesByJobAndTaskAsPdf(string userScope, int[] userIds, string jobScope, int[] jobIds, DateTime min, DateTime max, string commitStatus, User currentUser)
        {
            var buffer = new byte[0];
            var output = new MemoryStream();
            var tz = DateTimeZoneProviders.Tzdb.GetZoneOrNull(currentUser.TimeZone);
            var nowInstant = SystemClock.Instance.GetCurrentInstant();
            var nowLocal = nowInstant.InZone(tz);
            var nowDateTime = nowLocal.LocalDateTime.ToDateTimeUnspecified();

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
                    min.ToShortDateString(),
                    max.ToShortDateString()));

                // Header
                writer.PageEvent = new Header(
                    string.Format("REPORT: PUNCHES BY JOB AND TASK {0} thru {1}",
                        min.ToShortDateString(),
                        max.ToShortDateString()),
                    nowDateTime);

                // Open the document to enable you to write to the document
                document.Open();

                // Build table of punches
                PdfPTable table = new PdfPTable(8);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 9, 7, 9, 7, 26, 26, 7, 9 });

                // Get all the job ids for this organization
                // if the job scope is not for specific job ids
                if (jobScope != "specific")
                {
                    var customerIds = db.Customers
                        .Where(c => c.OrganizationId == currentUser.OrganizationId)
                        .Select(c => c.Id);
                    jobIds = db.Jobs
                        .Where(j => customerIds.Contains(j.CustomerId))
                        .Select(j => j.Id)
                        .ToArray();
                }

                // Get all the job ids for this organization
                // if the job scope is not for specific job ids
                IQueryable<Punch> punchesQueryable = db.Punches
                    .Include("Task")
                    .Include("Task.Job")
                    .Where(p => p.OutAt.HasValue == true)
                    .Where(p => p.InAt >= min && p.OutAt.Value <= max)
                    .Where(p => jobIds.Contains(p.Task.JobId));

                // Filter by user
                if (userScope == "specific")
                {
                    punchesQueryable = punchesQueryable
                        .Where(p => userIds.Contains(p.UserId));
                }

                // Filter by commit
                if (commitStatus == "only")
                {
                    punchesQueryable = punchesQueryable.Where(p => p.CommitId != null);
                }
                else if (commitStatus == "uncommitted")
                {
                    punchesQueryable = punchesQueryable.Where(p => p.CommitId == null);
                }

                var punches = punchesQueryable
                    .OrderBy(p => p.InAt)
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
                    var jobCell = new PdfPCell(new Paragraph(string.Format("Job {0} - {1} for Customer {2} - {3}", job.Number, job.Name, job.Customer.Number, job.Customer.Name), fontH1))
                    {
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        BackgroundColor = BaseColor.BLACK,
                        Colspan = 8,
                        Padding = 5,
                        PaddingBottom = 15
                    };
                    table.AddCell(jobCell);
                    
                    // Loop each date and print each punch
                    var dates = punches
                        .GroupBy(p => p.InAt.Date)
                        .Select(g => new {
                            Date = g.Key
                        })
                        .ToList();
                    foreach (var date in dates)
                    {
                        // Day
                        var dayCell = new PdfPCell(new Paragraph(date.Date.ToString("D"), fontH1))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            BackgroundColor = BaseColor.BLACK,
                            Colspan = 8,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(dayCell);

                        // Column Headers
                        var inHeaderCell = new PdfPCell(new Paragraph("In", fontH3))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(inHeaderCell);

                        var sourceInHeaderCell = new PdfPCell(new Paragraph("Src", fontH3))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(sourceInHeaderCell);

                        var outHeaderCell = new PdfPCell(new Paragraph("Out", fontH3))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(outHeaderCell);

                        var sourceOutHeaderCell = new PdfPCell(new Paragraph("Src", fontH3))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(sourceOutHeaderCell);

                        var userHeaderCell = new PdfPCell(new Paragraph("User", fontH3))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(userHeaderCell);

                        var taskHeaderCell = new PdfPCell(new Paragraph("Task", fontH3))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(taskHeaderCell);

                        var committedHeaderCell = new PdfPCell(new Phrase("Cmted", fontH3))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(committedHeaderCell);

                        var totalHeaderCell = new PdfPCell(new Paragraph("Total", fontH3))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(totalHeaderCell);

                        // Punches for this job
                        var punchesForJobAndDay = punches
                            .Where(p => p.InAt.Date == date.Date)
                            .Where(p => groupedTaskIds.Contains(p.TaskId))
                            .ToList();
                        foreach (var punch in punchesForJobAndDay)
                        {
                            // In At
                            var inCell = new PdfPCell(new Paragraph(string.Format("{0}", punch.InAt.ToString("h:mmtt").ToLower()), fontP))
                            {
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                Padding = 5,
                                UseAscender = true
                            };
                            table.AddCell(inCell);

                            // In At Source
                            var sourceForInAt = string.IsNullOrEmpty(punch.SourceForInAt) ? "" : punch.SourceForInAt[0].ToString();
                            var sourceInCell = new PdfPCell(new Paragraph(sourceForInAt.ToString(), fontP))
                            {
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                Padding = 5,
                                UseAscender = true
                            };
                            table.AddCell(sourceInCell);

                            // Out At
                            var outCell = new PdfPCell(new Paragraph(string.Format("{0}", punch.OutAt.Value.ToString("h:mmtt").ToLower()), fontP))
                            {
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                Padding = 5,
                                UseAscender = true
                            };
                            table.AddCell(outCell);

                            // Out At Source
                            var sourceForOutAt = string.IsNullOrEmpty(punch.SourceForOutAt) ? "" : punch.SourceForOutAt[0].ToString();
                            var sourceOutCell = new PdfPCell(new Paragraph(sourceForOutAt, fontP))
                            {
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                Padding = 5,
                                UseAscender = true
                            };
                            table.AddCell(sourceOutCell);

                            // User
                            var userCell = new PdfPCell(new Paragraph(punch.User.Name, fontP))
                            {
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                Padding = 5,
                                UseAscender = true
                            };
                            table.AddCell(userCell);

                            // Task
                            var taskCell = new PdfPCell(new Paragraph(string.Format("{0} - {1}", punch.Task.Number, punch.Task.Name), fontP))
                            {
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                Padding = 5,
                                UseAscender = true
                            };
                            table.AddCell(taskCell);

                            // Committed
                            var committed = punch.CommitId.HasValue ? "X" : "";
                            var committedCell = new PdfPCell(new Phrase(committed, fontP))
                            {
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                HorizontalAlignment = Element.ALIGN_CENTER,
                                Padding = 5,
                                UseAscender = true
                            };
                            table.AddCell(committedCell);

                            // Total
                            var total = Math.Round((punch.OutAt.Value - punch.InAt).TotalMinutes / 60, 2).ToString("0.00");
                            var totalCell = new PdfPCell(new Paragraph(total.ToString(), fontP))
                            {
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 5,
                                UseAscender = true
                            };
                            table.AddCell(totalCell);
                        }

                        // Daily Total
                        double dailyTotalMinutes = 0;
                        foreach (var punch in punchesForJobAndDay)
                        {
                            dailyTotalMinutes += (punch.OutAt.Value - punch.InAt).TotalMinutes;
                        }
                        var dailyTotal = Math.Round(dailyTotalMinutes / 60, 2).ToString("0.00");
                        var dailyTotalHeaderCell = new PdfPCell(new Paragraph("Daily Total", fontH3))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Colspan = 7,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(dailyTotalHeaderCell);
                        var dailyTotalValueCell = new PdfPCell(new Paragraph(dailyTotal.ToString(), fontH3))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(dailyTotalValueCell);
                    }
                }

                // Add a message for no punches
                if (punches.Count() == 0)
                {
                    document.Add(new Phrase(
                        string.Format("There are no punches between {0} and {1}",
                            min.ToShortDateString(),
                            max.ToShortDateString()),
                        fontP));
                }

                document.Add(table);

                // Make sure data has been written
                writer.Flush();

                // Close the document
                document.Close();
            }

            buffer = output.ToArray();

            // Page count must be added later due to nuances in iText
            AddPageNumbers(buffer);

            return buffer;
        }

        public byte[] PunchesByDayAsPdf(string userScope, int[] userIds, string jobScope, int[] jobIds, DateTime min, DateTime max, string commitStatus, User currentUser)
        {
            var buffer = new byte[0];
            var output = new MemoryStream();
            var tz = DateTimeZoneProviders.Tzdb.GetZoneOrNull(currentUser.TimeZone);
            var nowInstant = SystemClock.Instance.GetCurrentInstant();
            var nowLocal = nowInstant.InZone(tz);
            var nowDateTime = nowLocal.LocalDateTime.ToDateTimeUnspecified();

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
                    min.ToShortDateString(),
                    max.ToShortDateString()));

                // Header
                writer.PageEvent = new Header(
                    string.Format("REPORT: PUNCHES BY DAY {0} thru {1}",
                        min.ToShortDateString(),
                        max.ToShortDateString()),
                    nowDateTime);

                // Open the document to enable you to write to the document
                document.Open();

                // Build table of punches
                PdfPTable table = new PdfPTable(10);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 6, 4, 6, 4, 17, 17, 17, 17, 6, 6 });
                
                // Get all the job ids for this organization
                // if the job scope is not for specific job ids
                if (jobScope != "specific")
                {
                    var customerIds = db.Customers
                        .Where(c => c.OrganizationId == currentUser.OrganizationId)
                        .Select(c => c.Id);
                    jobIds = db.Jobs
                        .Where(j => customerIds.Contains(j.CustomerId))
                        .Select(j => j.Id)
                        .ToArray();
                }

                // Get all the punches for either specific users
                // or all the users in this organization
                IQueryable<Punch> punchesQueryable = db.Punches
                    .Include("Task")
                    .Include("Task.Job")
                    .Where(p => p.OutAt.HasValue == true)
                    .Where(p => p.InAt >= min && p.OutAt.Value <= max)
                    .Where(p => jobIds.Contains(p.Task.JobId));

                // Filter by user
                if (userScope == "specific")
                {
                    punchesQueryable = punchesQueryable
                        .Where(p => userIds.Contains(p.UserId));
                }

                // Filter by commit
                if (commitStatus == "only")
                {
                    punchesQueryable = punchesQueryable.Where(p => p.CommitId != null);
                }
                else if (commitStatus == "uncommitted")
                {
                    punchesQueryable = punchesQueryable.Where(p => p.CommitId == null);
                }

                var punches = punchesQueryable
                    .OrderBy(p => p.InAt)
                    .ToList();
                
                // Loop each date and print each punch
                var dates = punches
                    .GroupBy(p => p.InAt.Date)
                    .Select(g => new {
                        Date = g.Key
                    })
                    .ToList();
                foreach (var date in dates)
                {
                    // Day
                    var dayCell = new PdfPCell(new Paragraph(date.Date.ToString("D"), fontH1));
                    dayCell.Colspan = 10;
                    dayCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    dayCell.Padding = 5;
                    dayCell.PaddingBottom = 15;
                    dayCell.BackgroundColor = BaseColor.BLACK;
                    table.AddCell(dayCell);

                    // Column Headers
                    var inHeaderCell = new PdfPCell(new Paragraph("In", fontH3))
                    {
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 5,
                        UseAscender = true
                    };
                    table.AddCell(inHeaderCell);

                    var sourceInHeaderCell = new PdfPCell(new Paragraph("Src", fontH3))
                    {
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 5,
                        UseAscender = true
                    };
                    table.AddCell(sourceInHeaderCell);

                    var outHeaderCell = new PdfPCell(new Paragraph("Out", fontH3))
                    {
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 5,
                        UseAscender = true
                    };
                    table.AddCell(outHeaderCell);

                    var sourceOutHeaderCell = new PdfPCell(new Paragraph("Src", fontH3))
                    {
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 5,
                        UseAscender = true
                    };
                    table.AddCell(sourceOutHeaderCell);

                    var userHeaderCell = new PdfPCell(new Paragraph("User", fontH3))
                    {
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 5,
                        UseAscender = true
                    };
                    table.AddCell(userHeaderCell);

                    var customerHeaderCell = new PdfPCell(new Paragraph("Customer", fontH3))
                    {
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 5,
                        UseAscender = true
                    };
                    table.AddCell(customerHeaderCell);

                    var jobHeaderCell = new PdfPCell(new Paragraph("Job", fontH3))
                    {
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 5,
                        UseAscender = true
                    };
                    table.AddCell(jobHeaderCell);

                    var taskHeaderCell = new PdfPCell(new Paragraph("Task", fontH3))
                    {
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 5,
                        UseAscender = true
                    };
                    table.AddCell(taskHeaderCell);

                    var committedHeaderCell = new PdfPCell(new Phrase("Cmted", fontH3))
                    {
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 5,
                        UseAscender = true
                    };
                    table.AddCell(committedHeaderCell);

                    var totalHeaderCell = new PdfPCell(new Paragraph("Total", fontH3))
                    {
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 5,
                        UseAscender = true
                    };
                    table.AddCell(totalHeaderCell);

                    // Punches for this Day
                    var punchesForDay = punches
                        .Where(p => p.InAt.Date == date.Date)
                        .ToList();
                    foreach (var punch in punchesForDay)
                    {
                        // In At
                        var inCell = new PdfPCell(new Paragraph(punch.InAt.ToString("h:mmtt").ToLower(), fontP))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(inCell);

                        // In At Source
                        var sourceForInAt = string.IsNullOrEmpty(punch.SourceForInAt) ? "" : punch.SourceForInAt[0].ToString();
                        var sourceInCell = new PdfPCell(new Paragraph(sourceForInAt, fontP))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(sourceInCell);

                        // Out At
                        var outCell = new PdfPCell(new Paragraph(punch.OutAt.Value.ToString("h:mmtt").ToLower(), fontP))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(outCell);

                        // Out At Source
                        var sourceForOutAt = string.IsNullOrEmpty(punch.SourceForOutAt) ? "" : punch.SourceForOutAt[0].ToString();
                        var sourceOutCell = new PdfPCell(new Paragraph(sourceForOutAt, fontP))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(sourceOutCell);

                        // User
                        var userCell = new PdfPCell(new Paragraph(punch.User.Name, fontP))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(userCell);

                        // Customer
                        var customerCell = new PdfPCell(new Paragraph(string.Format("{0} - {1}", punch.Task.Job.Customer.Number, punch.Task.Job.Customer.Name), fontP))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(customerCell);

                        // Job
                        var jobCell = new PdfPCell(new Paragraph(string.Format("{0} - {1}", punch.Task.Job.Number, punch.Task.Job.Name), fontP))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(jobCell);

                        // Task
                        var taskCell = new PdfPCell(new Paragraph(string.Format("{0} - {1}", punch.Task.Number, punch.Task.Name), fontP))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(taskCell);

                        // Committed
                        var committed = punch.CommitId.HasValue ? "X" : "";
                        var committedCell = new PdfPCell(new Phrase(committed, fontP))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(committedCell);

                        // Total
                        var total = Math.Round((punch.OutAt.Value - punch.InAt).TotalMinutes / 60, 2).ToString("0.00");
                        var totalCell = new PdfPCell(new Paragraph(total.ToString(), fontP))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(totalCell);
                    }

                    // Daily Total
                    double dailyTotalMinutes = 0;
                    foreach (var punch in punchesForDay)
                    {
                        dailyTotalMinutes += (punch.OutAt.Value - punch.InAt).TotalMinutes;
                    }
                    var dailyTotal = Math.Round(dailyTotalMinutes / 60, 2).ToString("0.00");
                    var dailyTotalHeaderCell = new PdfPCell(new Paragraph("Daily Total", fontH3))
                    {
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        HorizontalAlignment = Element.ALIGN_RIGHT,
                        Padding = 5,
                        Colspan = 9,
                        UseAscender = true
                    };
                    table.AddCell(dailyTotalHeaderCell);
                    var dailyTotalValueCell = new PdfPCell(new Paragraph(dailyTotal.ToString(), fontH3))
                    {
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        HorizontalAlignment = Element.ALIGN_RIGHT,
                        Padding = 5,
                        UseAscender = true
                    };
                    table.AddCell(dailyTotalValueCell);
                }
                
                // Add a message for no punches
                if (punches.Count() == 0)
                {
                    document.Add(new Phrase(
                        string.Format("There are no punches between {0} and {1}",
                            min.ToShortDateString(),
                            max.ToShortDateString()),
                        fontP));
                }

                document.Add(table);

                // Make sure data has been written
                writer.Flush();

                // Close the document
                document.Close();
            }

            buffer = output.ToArray();

            // Page count must be added later due to nuances in iText
            AddPageNumbers(buffer);

            return buffer;
        }

        public byte[] TimeEntriesByUserAsPdf(string userScope, int[] userIds, string jobScope, int[] jobIds, DateTime min, DateTime max, User currentUser)
        {
            var buffer = new byte[0];
            var output = new MemoryStream();
            var tz = DateTimeZoneProviders.Tzdb.GetZoneOrNull(currentUser.TimeZone);
            var nowInstant = SystemClock.Instance.GetCurrentInstant();
            var nowLocal = nowInstant.InZone(tz);
            var nowDateTime = nowLocal.LocalDateTime.ToDateTimeUnspecified();

            // Create an instance of document which represents the PDF document itself
            using (var document = new Document(PageSize.LETTER.Rotate(), 30, 30, 60, 60))
            {
                var writer = PdfWriter.GetInstance(document, output);
                pageWidth = document.PageSize.Width;

                // Add meta information to the document
                document.AddAuthor(currentUser.Name);
                document.AddCreator("BRIZBEE");
                document.AddTitle(string.Format(
                    "Time Entries by User {0} thru {1}.pdf",
                    min.ToShortDateString(),
                    max.ToShortDateString()));

                // Header
                writer.PageEvent = new Header(
                    string.Format("REPORT: TIME ENTRIES BY USER {0} thru {1}",
                        min.ToShortDateString(),
                        max.ToShortDateString()),
                    nowDateTime);

                // Open the document to enable you to write to the document
                document.Open();

                // Build table of punches
                PdfPTable table = new PdfPTable(4);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 25, 25, 25, 25 });

                // Get the users depending on the filtered scope
                List<User> users;
                if (userScope == "specific")
                {
                    users = db.Users
                        .Where(u => u.OrganizationId == currentUser.OrganizationId)
                        .Where(u => userIds.Contains(u.Id))
                        .OrderBy(u => u.Name)
                        .ToList();
                }
                else
                {
                    users = db.Users
                        .Where(u => u.OrganizationId == currentUser.OrganizationId)
                        .OrderBy(u => u.Name)
                        .ToList();
                }
                foreach (var user in users)
                {
                    // User Name and Spacer
                    var nameCell = new PdfPCell(new Phrase(string.Format("User {0}", user.Name), fontH1))
                    {
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Colspan = 4,
                        Padding = 5,
                        PaddingTop = 8,
                        PaddingBottom = 8,
                        BackgroundColor = BaseColor.BLACK,
                        UseAscender = true
                    };
                    table.AddCell(nameCell);
                    var nameSpacerCell = new PdfPCell(new Phrase(" "))
                    {
                        Colspan = 4,
                        Padding = 0,
                        PaddingBottom = 8,
                        BorderWidthLeft = 0,
                        BorderWidthRight = 0,
                        UseAscender = true
                    };
                    table.AddCell(nameSpacerCell);

                    // Get all the time entries for the user
                    // either for any job or a specific job
                    IQueryable<TimesheetEntry> timeEntriesQueryable = db.TimesheetEntries
                        .Include("Task")
                        .Where(t => t.EnteredAt >= min && t.EnteredAt <= max)
                        .Where(t => t.UserId == user.Id);

                    // Filter by job
                    if (jobScope == "specific")
                    {
                        timeEntriesQueryable = timeEntriesQueryable
                            .Where(t => jobIds.Contains(t.Task.JobId));
                    }

                    var timeEntries = timeEntriesQueryable
                        .OrderBy(t => t.EnteredAt)
                        .ToList();

                    if (timeEntries.Count() == 0)
                    {
                        // Add a message if there are no time entries
                        var noneCell = new PdfPCell(new Phrase(
                            string.Format("There are no time entries for {0} between {1} and {2}",
                                user.Name,
                                min.ToShortDateString(),
                                max.ToShortDateString()),
                            fontP))
                        {
                            Colspan = 4,
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(noneCell);
                    }

                    // Loop each date and print each punch
                    var dates = timeEntries
                        .GroupBy(t => t.EnteredAt.Date)
                        .Select(g => new {
                            Date = g.Key
                        })
                        .ToList();
                    foreach (var date in dates)
                    {
                        // Day
                        var dayCell = new PdfPCell(new Phrase(date.Date.ToString("D"), fontH1))
                        {
                            Colspan = 4,
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            BackgroundColor = BaseColor.BLACK,
                            UseAscender = true
                        };
                        table.AddCell(dayCell);

                        // Column Headers
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
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(totalHeaderCell);

                        // Time Entries for this Day
                        var timeEntriesForDay = timeEntries
                            .Where(e => e.EnteredAt.Date == date.Date)
                            .ToList();
                        foreach (var timeEntry in timeEntriesForDay)
                        {
                            // Task
                            var taskCell = new PdfPCell(new Phrase(string.Format("{0} - {1}", timeEntry.Task.Number, timeEntry.Task.Name), fontP))
                            {
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                Padding = 5,
                                UseAscender = true
                            };
                            table.AddCell(taskCell);

                            // Job
                            var jobCell = new PdfPCell(new Phrase(string.Format("{0} - {1}", timeEntry.Task.Job.Number, timeEntry.Task.Job.Name), fontP))
                            {
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                Padding = 5,
                                UseAscender = true
                            };
                            table.AddCell(jobCell);

                            // Customer
                            var customerCell = new PdfPCell(new Phrase(string.Format("{0} - {1}", timeEntry.Task.Job.Customer.Number, timeEntry.Task.Job.Customer.Name), fontP))
                            {
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                Padding = 5,
                                UseAscender = true
                            };
                            table.AddCell(customerCell);

                            // Total
                            var total = Math.Round((double)timeEntry.Minutes / 60, 2).ToString("0.00");
                            var totalCell = new PdfPCell(new Phrase(total.ToString(), fontP))
                            {
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 5,
                                UseAscender = true
                            };
                            table.AddCell(totalCell);
                        }

                        // Daily Total and Spacer
                        double dailyTotalMinutes = 0;
                        foreach (var timeEntry in timeEntriesForDay)
                        {
                            dailyTotalMinutes += timeEntry.Minutes;
                        }
                        var dailyTotal = Math.Round(dailyTotalMinutes / 60, 2).ToString("0.00");
                        var dailyTotalHeaderCell = new PdfPCell(new Phrase("Daily Total", fontH3))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Colspan = 3,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(dailyTotalHeaderCell);
                        var dailyTotalValueCell = new PdfPCell(new Phrase(dailyTotal.ToString(), fontH3))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(dailyTotalValueCell);
                        var dailyTotalSpacerCell = new PdfPCell(new Phrase(" "))
                        {
                            Colspan = 9,
                            Padding = 0,
                            PaddingBottom = 4,
                            BorderWidthLeft = 0,
                            BorderWidthRight = 0,
                            UseAscender = true
                        };
                        table.AddCell(dailyTotalSpacerCell);
                    }

                    // User Total
                    double userTotalMinutes = 0;
                    foreach (var timeEntry in timeEntries)
                    {
                        userTotalMinutes += timeEntry.Minutes;
                    }
                    var userTotal = Math.Round(userTotalMinutes / 60, 2).ToString("0.00");
                    var userTotalHeaderCell = new PdfPCell(new Phrase(string.Format("Total for User {0}", user.Name), fontH3))
                    {
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        HorizontalAlignment = Element.ALIGN_RIGHT,
                        Colspan = 3,
                        Padding = 5,
                        UseAscender = true
                    };
                    table.AddCell(userTotalHeaderCell);
                    var userTotalValueCell = new PdfPCell(new Phrase(userTotal.ToString(), fontH3))
                    {
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        HorizontalAlignment = Element.ALIGN_RIGHT,
                        Padding = 5,
                        UseAscender = true
                    };
                    table.AddCell(userTotalValueCell);

                    var totalSpacerCell = new PdfPCell(new Phrase(" "))
                    {
                        Colspan = 4,
                        Padding = 0,
                        PaddingBottom = 8,
                        BorderWidthLeft = 0,
                        BorderWidthRight = 0,
                        UseAscender = true
                    };
                    table.AddCell(totalSpacerCell);

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
                        string.Format("There are no users with time entries between {0} and {1}",
                            min.ToShortDateString(),
                            max.ToShortDateString()),
                        fontP));
                }

                document.Add(table);

                // Make sure data has been written
                writer.Flush();

                // Close the document
                document.Close();
            }

            buffer = output.ToArray();

            // Page count must be added later due to nuances in iText
            AddPageNumbers(buffer);

            return buffer;
        }

        public byte[] TimeEntriesByJobAndTaskAsPdf(string userScope, int[] userIds, string jobScope, int[] jobIds, DateTime min, DateTime max, User currentUser)
        {
            var buffer = new byte[0];
            var output = new MemoryStream();
            var tz = DateTimeZoneProviders.Tzdb.GetZoneOrNull(currentUser.TimeZone);
            var nowInstant = SystemClock.Instance.GetCurrentInstant();
            var nowLocal = nowInstant.InZone(tz);
            var nowDateTime = nowLocal.LocalDateTime.ToDateTimeUnspecified();

            // Create an instance of document which represents the PDF document itself
            using (var document = new Document(PageSize.LETTER.Rotate(), 30, 30, 60, 60))
            {
                var writer = PdfWriter.GetInstance(document, output);
                pageWidth = document.PageSize.Width;

                // Add meta information to the document
                document.AddAuthor(currentUser.Name);
                document.AddCreator("BRIZBEE");
                document.AddTitle(string.Format(
                    "Time Entries by Job and Task {0} thru {1}.pdf",
                    min.ToShortDateString(),
                    max.ToShortDateString()));

                // Header
                writer.PageEvent = new Header(
                    string.Format("REPORT: TIME ENTRIES BY JOB AND TASK {0} thru {1}",
                        min.ToShortDateString(),
                        max.ToShortDateString()),
                    nowDateTime);

                // Open the document to enable you to write to the document
                document.Open();

                // Build table of punches
                PdfPTable table = new PdfPTable(3);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 35, 35, 30 });

                // Get all the job ids for this organization
                // if the job scope is not for specific job ids
                if (jobScope != "specific")
                {
                    var customerIds = db.Customers
                        .Where(c => c.OrganizationId == currentUser.OrganizationId)
                        .Select(c => c.Id);
                    jobIds = db.Jobs
                        .Where(j => customerIds.Contains(j.CustomerId))
                        .Select(j => j.Id)
                        .ToArray();
                }

                // Get all the time entries for the user
                // either for any job or a specific job
                IQueryable<TimesheetEntry> timeEntriesQueryable = db.TimesheetEntries
                    .Include("Task")
                    .Where(t => t.EnteredAt >= min && t.EnteredAt <= max)
                    .Where(t => jobIds.Contains(t.Task.JobId));

                // Filter by user
                if (userScope == "specific")
                {
                    timeEntriesQueryable = timeEntriesQueryable
                        .Where(t => userIds.Contains(t.UserId));
                }

                var timeEntries = timeEntriesQueryable
                    .OrderBy(t => t.EnteredAt)
                    .ToList();

                var groupedJobIds = timeEntries
                    .GroupBy(t => t.Task.JobId)
                    .Select(g => g.Key)
                    .ToList();

                foreach (var jobId in groupedJobIds)
                {
                    var job = db.Jobs.Find(jobId);
                    var groupedTaskIds = timeEntries
                        .GroupBy(t => t.TaskId)
                        .Select(g => g.Key)
                        .ToList();

                    // Job Name
                    var jobCell = new PdfPCell(new Paragraph(string.Format("Job {0} - {1} for Customer {2} - {3}", job.Number, job.Name, job.Customer.Number, job.Customer.Name), fontH1))
                    {
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        BackgroundColor = BaseColor.BLACK,
                        Colspan = 3,
                        Padding = 5,
                        PaddingBottom = 15
                    };
                    table.AddCell(jobCell);

                    // Loop each date and print each time entry
                    var dates = timeEntries
                        .GroupBy(t => t.EnteredAt.Date)
                        .Select(g => new
                        {
                            Date = g.Key
                        })
                        .ToList();
                    foreach (var date in dates)
                    {
                        // Day
                        var dayCell = new PdfPCell(new Paragraph(date.Date.ToString("D"), fontH1))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            BackgroundColor = BaseColor.BLACK,
                            Colspan = 3,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(dayCell);

                        // Column Headers
                        var userHeaderCell = new PdfPCell(new Paragraph("User", fontH3))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(userHeaderCell);

                        var taskHeaderCell = new PdfPCell(new Paragraph("Task", fontH3))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(taskHeaderCell);

                        var totalHeaderCell = new PdfPCell(new Paragraph("Total", fontH3))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(totalHeaderCell);

                        // Time entries for this job
                        var timeEntriesForJobAndDay = timeEntries
                            .Where(t => t.EnteredAt.Date == date.Date)
                            .Where(t => groupedTaskIds.Contains(t.TaskId))
                            .ToList();
                        foreach (var timeEntry in timeEntriesForJobAndDay)
                        {
                            // User
                            var userCell = new PdfPCell(new Paragraph(timeEntry.User.Name, fontP))
                            {
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                Padding = 5,
                                UseAscender = true
                            };
                            table.AddCell(userCell);

                            // Task
                            var taskCell = new PdfPCell(new Paragraph(string.Format("{0} - {1}", timeEntry.Task.Number, timeEntry.Task.Name), fontP))
                            {
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                Padding = 5,
                                UseAscender = true
                            };
                            table.AddCell(taskCell);

                            // Total
                            var dbl = double.Parse(timeEntry.Minutes.ToString());
                            var total = Math.Round(dbl / 60, 2).ToString("0.00");
                            var totalCell = new PdfPCell(new Paragraph(total.ToString(), fontP))
                            {
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 5,
                                UseAscender = true
                            };
                            table.AddCell(totalCell);
                        }

                        // Daily Total
                        double dailyTotalMinutes = 0;
                        foreach (var timeEntry in timeEntriesForJobAndDay)
                        {
                            dailyTotalMinutes += timeEntry.Minutes;
                        }
                        var dailyTotal = Math.Round(dailyTotalMinutes / 60, 2).ToString("0.00");
                        var dailyTotalHeaderCell = new PdfPCell(new Paragraph("Daily Total", fontH3))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Colspan = 2,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(dailyTotalHeaderCell);
                        var dailyTotalValueCell = new PdfPCell(new Paragraph(dailyTotal.ToString(), fontH3))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(dailyTotalValueCell);
                    }
                }

                // Add a message for no time entries
                if (timeEntries.Count() == 0)
                {
                    document.Add(new Phrase(
                        string.Format("There are no time entries between {0} and {1}",
                            min.ToShortDateString(),
                            max.ToShortDateString()),
                        fontP));
                }

                document.Add(table);

                // Make sure data has been written
                writer.Flush();

                // Close the document
                document.Close();
            }

            buffer = output.ToArray();

            // Page count must be added later due to nuances in iText
            AddPageNumbers(buffer);

            return buffer;
        }

        public byte[] TimeEntriesByDayAsPdf(string userScope, int[] userIds, string jobScope, int[] jobIds, DateTime min, DateTime max, User currentUser)
        {
            var buffer = new byte[0];
            var output = new MemoryStream();
            var tz = DateTimeZoneProviders.Tzdb.GetZoneOrNull(currentUser.TimeZone);
            var nowInstant = SystemClock.Instance.GetCurrentInstant();
            var nowLocal = nowInstant.InZone(tz);
            var nowDateTime = nowLocal.LocalDateTime.ToDateTimeUnspecified();

            // Create an instance of document which represents the PDF document itself
            using (var document = new Document(PageSize.LETTER.Rotate(), 30, 30, 60, 60))
            {
                var writer = PdfWriter.GetInstance(document, output);
                pageWidth = document.PageSize.Width;

                // Add meta information to the document
                document.AddAuthor(currentUser.Name);
                document.AddCreator("BRIZBEE");
                document.AddTitle(string.Format(
                    "Time Entries by Day {0} thru {1}.pdf",
                    min.ToShortDateString(),
                    max.ToShortDateString()));

                // Header
                writer.PageEvent = new Header(
                    string.Format("REPORT: TIME ENTRIES BY DAY {0} thru {1}",
                        min.ToShortDateString(),
                        max.ToShortDateString()),
                    nowDateTime);

                // Open the document to enable you to write to the document
                document.Open();

                // Build table of punches
                PdfPTable table = new PdfPTable(5);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 20, 20, 20, 20, 20 });

                // Get all the job ids for this organization
                // if the job scope is not for specific job ids
                if (jobScope != "specific")
                {
                    var customerIds = db.Customers
                        .Where(c => c.OrganizationId == currentUser.OrganizationId)
                        .Select(c => c.Id);
                    jobIds = db.Jobs
                        .Where(j => customerIds.Contains(j.CustomerId))
                        .Select(j => j.Id)
                        .ToArray();
                }

                // Get all the time entries for either specific users
                // or all the users in this organization
                IQueryable<TimesheetEntry> timeEntriesQueryable = db.TimesheetEntries
                    .Include("Task")
                    .Include("Task.Job")
                    .Where(t => t.EnteredAt >= min && t.EnteredAt <= max)
                    .Where(t => jobIds.Contains(t.Task.JobId));

                // Filter by user
                if (userScope == "specific")
                {
                    timeEntriesQueryable = timeEntriesQueryable
                        .Where(t => userIds.Contains(t.UserId));
                }

                var timeEntries = timeEntriesQueryable
                    .OrderBy(t => t.EnteredAt)
                    .ToList();

                // Loop each date and print each punch
                var dates = timeEntries
                    .GroupBy(t => t.EnteredAt.Date)
                    .Select(g => new {
                        Date = g.Key
                    })
                    .ToList();
                foreach (var date in dates)
                {
                    // Day
                    var dayCell = new PdfPCell(new Paragraph(date.Date.ToString("D"), fontH1));
                    dayCell.Colspan = 5;
                    dayCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    dayCell.Padding = 5;
                    dayCell.PaddingBottom = 15;
                    dayCell.BackgroundColor = BaseColor.BLACK;
                    table.AddCell(dayCell);

                    // Column Headers
                    var userHeaderCell = new PdfPCell(new Paragraph("User", fontH3))
                    {
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 5,
                        UseAscender = true
                    };
                    table.AddCell(userHeaderCell);

                    var customerHeaderCell = new PdfPCell(new Paragraph("Customer", fontH3))
                    {
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 5,
                        UseAscender = true
                    };
                    table.AddCell(customerHeaderCell);

                    var jobHeaderCell = new PdfPCell(new Paragraph("Job", fontH3))
                    {
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 5,
                        UseAscender = true
                    };
                    table.AddCell(jobHeaderCell);

                    var taskHeaderCell = new PdfPCell(new Paragraph("Task", fontH3))
                    {
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 5,
                        UseAscender = true
                    };
                    table.AddCell(taskHeaderCell);

                    var totalHeaderCell = new PdfPCell(new Paragraph("Total", fontH3))
                    {
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 5,
                        UseAscender = true
                    };
                    table.AddCell(totalHeaderCell);

                    // Time Entries for this Day
                    var timeEntriesForDay = timeEntries
                        .Where(t => t.EnteredAt.Date == date.Date)
                        .ToList();
                    foreach (var timeEntry in timeEntriesForDay)
                    {
                        // User
                        var userCell = new PdfPCell(new Paragraph(timeEntry.User.Name, fontP))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(userCell);

                        // Customer
                        var customerCell = new PdfPCell(new Paragraph(string.Format("{0} - {1}", timeEntry.Task.Job.Customer.Number, timeEntry.Task.Job.Customer.Name), fontP))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(customerCell);

                        // Job
                        var jobCell = new PdfPCell(new Paragraph(string.Format("{0} - {1}", timeEntry.Task.Job.Number, timeEntry.Task.Job.Name), fontP))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(jobCell);

                        // Task
                        var taskCell = new PdfPCell(new Paragraph(string.Format("{0} - {1}", timeEntry.Task.Number, timeEntry.Task.Name), fontP))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(taskCell);

                        // Total
                        double minutes = timeEntry.Minutes;
                        var total = Math.Round(minutes / 60, 2).ToString("0.00");
                        var totalCell = new PdfPCell(new Paragraph(total.ToString(), fontP))
                        {
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 5,
                            UseAscender = true
                        };
                        table.AddCell(totalCell);
                    }

                    // Daily Total
                    double dailyTotalMinutes = 0;
                    foreach (var timeEntry in timeEntriesForDay)
                    {
                        dailyTotalMinutes += timeEntry.Minutes;
                    }
                    var dailyTotal = Math.Round(dailyTotalMinutes / 60, 2).ToString("0.00");
                    var dailyTotalHeaderCell = new PdfPCell(new Paragraph("Daily Total", fontH3))
                    {
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        HorizontalAlignment = Element.ALIGN_RIGHT,
                        Padding = 5,
                        Colspan = 4,
                        UseAscender = true
                    };
                    table.AddCell(dailyTotalHeaderCell);
                    var dailyTotalValueCell = new PdfPCell(new Paragraph(dailyTotal.ToString(), fontH3))
                    {
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        HorizontalAlignment = Element.ALIGN_RIGHT,
                        Padding = 5,
                        UseAscender = true
                    };
                    table.AddCell(dailyTotalValueCell);
                }

                // Add a message for no time entries
                if (timeEntries.Count() == 0)
                {
                    document.Add(new Phrase(
                        string.Format("There are no time entries between {0} and {1}",
                            min.ToShortDateString(),
                            max.ToShortDateString()),
                        fontP));
                }

                document.Add(table);

                // Make sure data has been written
                writer.Flush();

                // Close the document
                document.Close();
            }

            buffer = output.ToArray();

            // Page count must be added later due to nuances in iText
            AddPageNumbers(buffer);

            return buffer;
        }

        public byte[] TasksByJobAsPdf(int jobId, User currentUser)
        {
            var buffer = new byte[0];
            var output = new MemoryStream();
            var tz = DateTimeZoneProviders.Tzdb.GetZoneOrNull(currentUser.TimeZone);
            var nowInstant = SystemClock.Instance.GetCurrentInstant();
            var nowLocal = nowInstant.InZone(tz);
            var nowDateTime = nowLocal.LocalDateTime.ToDateTimeUnspecified();
            var job = db.Jobs.Where(j => j.Id == jobId).FirstOrDefault();

            // Create an instance of document which represents the PDF document itself
            using (var document = new Document(PageSize.LETTER, 30, 30, 60, 60))
            {
                var writer = PdfWriter.GetInstance(document, output);
                pageWidth = document.PageSize.Width;

                // Add meta information to the document
                document.AddAuthor(currentUser.Name);
                document.AddCreator("BRIZBEE");
                document.AddTitle(string.Format(
                    "Tasks by Job for {0} - {1}.pdf",
                    job.Number,
                    job.Name));

                // Header
                writer.PageEvent = new Header(
                    string.Format("REPORT: TASKS BY JOB FOR {0} - {1}",
                        job.Number,
                        job.Name.ToUpper()),
                    nowDateTime);

                // Open the document to enable you to write to the document
                document.Open();

                // Build table of tasks
                PdfPTable table = new PdfPTable(2);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 50, 50 });

                // Job details
                var details = new PdfPCell();
                details.Padding = 20;
                var customerParagraph = new Paragraph(string.Format("CUSTOMER: {0} - {1}", job.Customer.Number, job.Customer.Name.ToUpper()), fontH4);
                customerParagraph.SpacingAfter = 0;
                var jobParagraph = new Paragraph(string.Format("JOB: {0} - {1}", job.Number, job.Name.ToUpper()), fontH4);
                jobParagraph.SpacingAfter = 10;
                var descriptionParagraph = new Paragraph(string.Format("DESCRIPTION: {0}", job.Description), fontP);
                details.AddElement(customerParagraph);
                details.AddElement(jobParagraph);
                details.AddElement(descriptionParagraph);
                details.Colspan = 2;
                table.AddCell(details);

                var tasks = db.Tasks.Where(t => t.JobId == jobId);
                foreach (var task in tasks)
                {
                    try
                    {
                        // Barcode
                        Barcode128 barCode = new Barcode128();
                        barCode.TextAlignment = Element.ALIGN_CENTER;
                        barCode.Code = task.Number;
                        barCode.StartStopText = false;
                        barCode.CodeType = iTextSharp.text.pdf.Barcode128.CODE128;
                        barCode.Extended = true;

                        // Barcode image
                        var drawing = barCode.CreateDrawingImage(System.Drawing.Color.Black, System.Drawing.Color.White);
                        iTextSharp.text.Image image = iTextSharp.text.Image.GetInstance(drawing, System.Drawing.Imaging.ImageFormat.Jpeg);
                        image.ScaleAbsoluteHeight(50);

                        // Cell
                        var cell = new PdfPCell();
                        cell.Padding = 20;
                        cell.HorizontalAlignment = PdfPCell.ALIGN_CENTER;
                        var code = new Paragraph();
                        code.Alignment = 1;
                        code.Add(new Chunk(image, 0, 0, true));
                        cell.AddElement(code);
                        var subtitle = new Paragraph();
                        subtitle.Alignment = 1;
                        subtitle.Add(new Chunk(string.Format("{0} - {1}", task.Number, task.Name.ToUpper()), fontH4));
                        cell.AddElement(subtitle);
                        table.AddCell(cell);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError(ex.ToString());
                    }
                }

                document.Add(table);

                // Make sure data has been written
                writer.Flush();

                // Close the document
                document.Close();
            }

            buffer = output.ToArray();

            // Page count must be added later due to nuances in iText
            AddPageNumbers(buffer);

            return buffer;
        }

        private void AddPageNumbers(byte[] buffer)
        {
            Trace.TraceInformation("Adding page numbers");
            using (MemoryStream stream = new MemoryStream())
            {
                PdfReader reader = new PdfReader(buffer);
                using (PdfStamper stamper = new PdfStamper(reader, stream))
                {
                    Trace.TraceInformation(reader.NumberOfPages.ToString());
                    int pages = reader.NumberOfPages;
                    for (int i = 1; i <= pages; i++)
                    {
                        Trace.TraceInformation("Looping page");
                        Trace.TraceInformation(pageWidth.ToString());
                        var text = string.Format("Page {0} of {1}", i, pages);
                        ColumnText.ShowTextAligned(stamper.GetOverContent(i), Element.ALIGN_RIGHT, new Phrase(text, fontFooter), pageWidth - 30f, 30f, 0);
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
            catch (DocumentException ex)
            {
                Trace.TraceError(ex.ToString());
            }
            catch (IOException ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }

        public override void OnEndPage(PdfWriter writer, Document document)
        {
            base.OnEndPage(writer, document);

            Font baseFontNormal = new Font(Font.FontFamily.HELVETICA, 9, Font.NORMAL, BaseColor.BLACK);
            Font baseFontBig = new Font(Font.FontFamily.HELVETICA, 9, Font.BOLD, BaseColor.BLACK);
            
            PdfPTable tableHeader = new PdfPTable(2);
            
            PdfPCell headerCell1 = new PdfPCell(new Paragraph(Title, baseFontNormal));
            PdfPCell headerCell2 = new PdfPCell(new Paragraph(string.Format("Generated {0}", PrintTime.ToString("F")), baseFontNormal));
            
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