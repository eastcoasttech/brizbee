using Brizbee.Common.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Brizbee.Services
{
    public class ReportBuilder
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();

        public byte[] PunchesByUserAsPdf(int[] userIds, DateTime min, DateTime max, User currentUser)
        {
            var buffer = new byte[0];
            var output = new MemoryStream();

            // Create an instance of the document class which represents the PDF document itself.
            using (var document = new Document(PageSize.LETTER.Rotate(), 20, 20, 20, 20))
            {
                var writer = PdfWriter.GetInstance(document, output);
                var fontH1 = new Font(Font.FontFamily.HELVETICA, 10, Font.BOLD);
                var fontH2 = new Font(Font.FontFamily.HELVETICA, 10, Font.BOLDITALIC);
                var fontP = new Font(Font.FontFamily.HELVETICA, 8, Font.NORMAL);

                // Add meta information to the document
                document.AddAuthor(currentUser.Name);
                document.AddCreator("BRIZBEE");
                document.AddTitle(string.Format(
                    "Punches by User {0} thru {1}.pdf",
                    min.ToShortDateString(),
                    max.ToShortDateString()));
                
                // Open the document to enable you to write to the document
                document.Open();

                // Build table of punches
                PdfPTable table = new PdfPTable(7);
                table.WidthPercentage = 100;
                document.Add(table);

                var users = db.Users.Where(u => userIds.Contains(u.Id)).OrderBy(u => u.Name).ToList();
                foreach (var user in users)
                {
                    Trace.TraceInformation("Looping " + user.Name);
                    // User Name
                    var nameCell = new PdfPCell(new Paragraph(user.Name, fontH1));
                    nameCell.Colspan = 7;
                    nameCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    nameCell.PaddingTop = 5;
                    nameCell.PaddingBottom = 5;
                    nameCell.PaddingLeft = 5;
                    nameCell.PaddingRight = 5;
                    table.AddCell(nameCell);
                    
                    var punches = db.Punches
                        .Where(p => p.OutAt.HasValue)
                        .Where(p => p.InAt >= min && p.OutAt <= max)
                        .Where(p => p.UserId == user.Id)
                        .OrderBy(p => p.InAt)
                        .ToList();
                    Trace.TraceInformation("Count of punches are " + punches.Count().ToString());
                    var dates = db.Punches
                        .Where(p => p.OutAt.HasValue)
                        .Where(p => p.InAt >= min && p.OutAt <= max)
                        .Where(p => p.UserId == user.Id)
                        .GroupBy(p => DbFunctions.TruncateTime(p.InAt))
                        .Select(g => new {
                            DateTime = g.Key.Value
                        })
                        .ToList();
                    Trace.TraceInformation("Count of days are " + dates.Count().ToString());
                    foreach (var date in dates)
                    {
                        // Day
                        var dayCell = new PdfPCell(new Paragraph(date.DateTime.ToString("D"), fontH2));
                        dayCell.Colspan = 7;
                        dayCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        dayCell.PaddingTop = 5;
                        dayCell.PaddingBottom = 5;
                        dayCell.PaddingLeft = 5;
                        dayCell.PaddingRight = 5;
                        table.AddCell(dayCell);
                        
                        // Punches for this Day
                        var punchesForDay = punches
                            .Where(p => p.InAt.Date == date.DateTime)
                            .ToList();
                        foreach (var punch in punchesForDay)
                        {
                            // In At
                            var inCell = new PdfPCell(new Paragraph(punch.InAt.ToString(), fontP));
                            inCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                            inCell.PaddingTop = 5;
                            inCell.PaddingBottom = 5;
                            inCell.PaddingLeft = 5;
                            inCell.PaddingRight = 5;
                            table.AddCell(inCell);

                            // In At Source
                            var sourceInCell = new PdfPCell(new Paragraph(punch.SourceForInAt, fontP));
                            sourceInCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                            sourceInCell.PaddingTop = 5;
                            sourceInCell.PaddingBottom = 5;
                            sourceInCell.PaddingLeft = 5;
                            sourceInCell.PaddingRight = 5;
                            table.AddCell(sourceInCell);

                            // Out At
                            var outCell = new PdfPCell(new Paragraph(punch.OutAt.ToString(), fontP));
                            outCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                            outCell.PaddingTop = 5;
                            outCell.PaddingBottom = 5;
                            outCell.PaddingLeft = 5;
                            outCell.PaddingRight = 5;
                            table.AddCell(outCell);

                            // Out At Source
                            var sourceOutCell = new PdfPCell(new Paragraph(punch.SourceForOutAt, fontP));
                            sourceOutCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                            sourceOutCell.PaddingTop = 5;
                            sourceOutCell.PaddingBottom = 5;
                            sourceOutCell.PaddingLeft = 5;
                            sourceOutCell.PaddingRight = 5;
                            table.AddCell(sourceOutCell);

                            // Task
                            var taskCell = new PdfPCell(new Paragraph(string.Format("{0} - {1}", punch.Task.Number, punch.Task.Name), fontP));
                            taskCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                            taskCell.PaddingTop = 5;
                            taskCell.PaddingBottom = 5;
                            taskCell.PaddingLeft = 5;
                            taskCell.PaddingRight = 5;
                            table.AddCell(taskCell);

                            // Job
                            var jobCell = new PdfPCell(new Paragraph(string.Format("{0} - {1}", punch.Task.Job.Number, punch.Task.Job.Name), fontP));
                            jobCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                            jobCell.PaddingTop = 5;
                            jobCell.PaddingBottom = 5;
                            jobCell.PaddingLeft = 5;
                            jobCell.PaddingRight = 5;
                            table.AddCell(jobCell);

                            // Customer
                            var customerCell = new PdfPCell(new Paragraph(string.Format("{0} - {1}", punch.Task.Job.Customer.Number, punch.Task.Job.Customer.Name), fontP));
                            customerCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                            customerCell.PaddingTop = 5;
                            customerCell.PaddingBottom = 5;
                            customerCell.PaddingLeft = 5;
                            customerCell.PaddingRight = 5;
                            table.AddCell(customerCell);
                        }
                    }

                    // Page break
                    if (users.Last() == user)
                    {
                        document.NewPage();
                    }
                }
                
                // Add a message for no users
                if (users.Count() == 0)
                {
                    document.Add(new Paragraph("There are no users with those IDs", fontP));
                }

                document.Add(table);

                // Make sure data has been written
                writer.Flush();

                // Close the document
                document.Close();
            }

            buffer = output.GetBuffer();
            return buffer;
        }

        public byte[] PunchesByJobAsPdf(int[] userIds, int[] jobIds, DateTime min, DateTime max, User currentUser)
        {
            var buffer = new byte[0];
            var output = new MemoryStream();

            buffer = output.GetBuffer();
            return buffer;
        }

        public byte[] PunchesByDayAsPdf(int[] userIds, int[] jobIds, DateTime min, DateTime max, User currentUser)
        {
            var buffer = new byte[0];
            var output = new MemoryStream();

            buffer = output.GetBuffer();
            return buffer;
        }
    }
}