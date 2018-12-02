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

        public byte[] PunchesByUserAsPdf(int[] userIds, DateTime min, DateTime max, User currentUser)
        {
            var buffer = new byte[0];
            var output = new MemoryStream();
            var organization = db.Organizations.Find(currentUser.OrganizationId);
            var tz = TimeZoneInfo.FindSystemTimeZoneById(organization.TimeZone);
            var fontTitle = new Font(Font.FontFamily.HELVETICA, 9, Font.BOLD);
            var fontH1 = new Font(Font.FontFamily.HELVETICA, 9, Font.BOLD);
            var fontH2 = new Font(Font.FontFamily.HELVETICA, 9, Font.BOLDITALIC);
            var fontH3 = new Font(Font.FontFamily.HELVETICA, 8, Font.BOLD);
            var fontP = new Font(Font.FontFamily.HELVETICA, 8, Font.NORMAL);
            var fontFooter = new Font(Font.FontFamily.HELVETICA, 9, Font.NORMAL);
            var width = 0f;

            // Create an instance of the document class which represents the PDF document itself.
            using (var document = new Document(PageSize.LETTER.Rotate(), 30, 30, 60, 30))
            {
                var writer = PdfWriter.GetInstance(document, output);
                width = document.PageSize.Width;

                // Add meta information to the document
                document.AddAuthor(currentUser.Name);
                document.AddCreator("BRIZBEE");
                document.AddTitle(string.Format(
                    "Punches by User {0} thru {1}.pdf",
                    TimeZoneInfo.ConvertTime(min, tz).ToShortDateString(),
                    TimeZoneInfo.ConvertTime(max, tz).ToShortDateString()));

                // Header
                writer.PageEvent = new ITextEvents(
                    string.Format("REPORT: PUNCHES BY USER {0} thru {1}",
                        TimeZoneInfo.ConvertTime(min, tz).ToShortDateString(),
                        TimeZoneInfo.ConvertTime(max, tz).ToShortDateString()),
                    TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz));

                //Header objHeaderFooter = new Header(new Phrase(string.Format("REPORT: PUNCHES BY USER {0} thru {1}",
                //    TimeZoneInfo.ConvertTime(min, tz).ToShortDateString(),
                //    TimeZoneInfo.ConvertTime(max, tz).ToShortDateString()), fontTitle));
                //writer.PageEvent = objHeaderFooter;

                // Open the document to enable you to write to the document
                document.Open();
                
                // Build table of punches
                PdfPTable table = new PdfPTable(8);
                table.WidthPercentage = 100;
                document.Add(table);

                var users = db.Users.Where(u => userIds.Contains(u.Id)).OrderBy(u => u.Name).ToList();
                foreach (var user in users)
                {
                    Trace.TraceInformation("Looping " + user.Name);
                    // User Name
                    var nameCell = new PdfPCell(new Paragraph(user.Name, fontH1));
                    nameCell.Colspan = 8;
                    nameCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    nameCell.Padding = 4;
                    table.AddCell(nameCell);
                    
                    var punches = db.Punches
                        .Where(p => p.OutAt.HasValue)
                        .Where(p => p.InAt >= min && p.OutAt <= max)
                        .Where(p => p.UserId == user.Id)
                        .OrderBy(p => p.InAt)
                        .ToList();
                    Trace.TraceInformation("Count of punches are " + punches.Count().ToString());
                    
                    // Add a message for no punches
                    if (punches.Count() == 0)
                    {
                        var noneCell = new PdfPCell(new Paragraph(
                            string.Format("There are no punches for {0} between {1} and {2}",
                                user.Name,
                                TimeZoneInfo.ConvertTime(min, tz).ToShortDateString(),
                                TimeZoneInfo.ConvertTime(max, tz).ToShortDateString()),
                            fontP));
                        noneCell.Colspan = 8;
                        noneCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        noneCell.Padding = 4;
                        table.AddCell(noneCell);
                    }
                    
                    // Loop each date and print each punch
                    var dates = punches
                        .GroupBy(p => TimeZoneInfo.ConvertTime(p.InAt, tz).Date)
                        .Select(g => new {
                            Date = g.Key
                        })
                        .ToList();
                    Trace.TraceInformation("Count of days are " + dates.Count().ToString());
                    foreach (var date in dates)
                    {
                        // Day
                        var dayCell = new PdfPCell(new Paragraph(date.Date.ToString("D"), fontH2));
                        dayCell.Colspan = 8;
                        dayCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        dayCell.Padding = 4;
                        table.AddCell(dayCell);

                        // Column Headers
                        var inHeaderCell = new PdfPCell(new Paragraph("In", fontH3));
                        inHeaderCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        inHeaderCell.Padding = 4;
                        table.AddCell(inHeaderCell);

                        var sourceInHeaderCell = new PdfPCell(new Paragraph("Source", fontH3));
                        sourceInHeaderCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        sourceInHeaderCell.Padding = 4;
                        table.AddCell(sourceInHeaderCell);

                        var outHeaderCell = new PdfPCell(new Paragraph("Out", fontH3));
                        outHeaderCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        outHeaderCell.Padding = 4;
                        table.AddCell(outHeaderCell);

                        var sourceOutHeaderCell = new PdfPCell(new Paragraph("Source", fontH3));
                        sourceOutHeaderCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        sourceOutHeaderCell.Padding = 4;
                        table.AddCell(sourceOutHeaderCell);

                        var taskHeaderCell = new PdfPCell(new Paragraph("Task", fontH3));
                        taskHeaderCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        taskHeaderCell.Padding = 4;
                        table.AddCell(taskHeaderCell);

                        var jobHeaderCell = new PdfPCell(new Paragraph("Job", fontH3));
                        jobHeaderCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        jobHeaderCell.Padding = 4;
                        table.AddCell(jobHeaderCell);

                        var customerHeaderCell = new PdfPCell(new Paragraph("Customer", fontH3));
                        customerHeaderCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        customerHeaderCell.Padding = 4;
                        table.AddCell(customerHeaderCell);

                        var totalHeaderCell = new PdfPCell(new Paragraph("Total", fontH3));
                        totalHeaderCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        totalHeaderCell.Padding = 4;
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
                            inCell.Padding = 4;
                            table.AddCell(inCell);

                            // In At Source
                            var sourceInCell = new PdfPCell(new Paragraph(punch.SourceForInAt, fontP));
                            sourceInCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                            sourceInCell.Padding = 4;
                            table.AddCell(sourceInCell);

                            // Out At
                            var outCell = new PdfPCell(new Paragraph(TimeZoneInfo.ConvertTime(punch.OutAt.Value, tz).ToString("h:mmtt").ToLower(), fontP));
                            outCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                            outCell.Padding = 4;
                            table.AddCell(outCell);

                            // Out At Source
                            var sourceOutCell = new PdfPCell(new Paragraph(punch.SourceForOutAt, fontP));
                            sourceOutCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                            sourceOutCell.Padding = 4;
                            table.AddCell(sourceOutCell);

                            // Task
                            var taskCell = new PdfPCell(new Paragraph(string.Format("{0} - {1}", punch.Task.Number, punch.Task.Name), fontP));
                            taskCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                            taskCell.Padding = 4;
                            table.AddCell(taskCell);

                            // Job
                            var jobCell = new PdfPCell(new Paragraph(string.Format("{0} - {1}", punch.Task.Job.Number, punch.Task.Job.Name), fontP));
                            jobCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                            jobCell.Padding = 4;
                            table.AddCell(jobCell);

                            // Customer
                            var customerCell = new PdfPCell(new Paragraph(string.Format("{0} - {1}", punch.Task.Job.Customer.Number, punch.Task.Job.Customer.Name), fontP));
                            customerCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                            customerCell.Padding = 4;
                            table.AddCell(customerCell);

                            // Total
                            var total = Math.Round((punch.OutAt.Value - punch.InAt).TotalMinutes / 60, 2);
                            var totalCell = new PdfPCell(new Paragraph(total.ToString(), fontP));
                            totalCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                            totalCell.Padding = 4;
                            table.AddCell(totalCell);
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
                    document.Add(new Paragraph(
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
            using (MemoryStream stream = new MemoryStream())
            {
                PdfReader reader = new PdfReader(buffer);
                using (PdfStamper stamper = new PdfStamper(reader, stream))
                {
                    int pages = reader.NumberOfPages;
                    for (int i = 1; i <= pages; i++)
                    {
                        var text = string.Format("Page {0} of {1}", i, pages);
                        ColumnText.ShowTextAligned(stamper.GetUnderContent(i), Element.ALIGN_RIGHT, new Phrase(text, fontFooter), width - 30, 30f, 0);
                    }
                }
                buffer = stream.ToArray();
            }

            return buffer;
        }

        public byte[] PunchesByJobAsPdf(int[] userIds, int[] jobIds, DateTime min, DateTime max, User currentUser)
        {
            var buffer = new byte[0];
            var output = new MemoryStream();
            var organization = db.Organizations.Find(currentUser.OrganizationId);
            var tz = TimeZoneInfo.FindSystemTimeZoneById(organization.TimeZone);

            buffer = output.GetBuffer();
            return buffer;
        }

        public byte[] PunchesByDayAsPdf(int[] userIds, int[] jobIds, DateTime min, DateTime max, User currentUser)
        {
            var buffer = new byte[0];
            var output = new MemoryStream();
            var organization = db.Organizations.Find(currentUser.OrganizationId);
            var tz = TimeZoneInfo.FindSystemTimeZoneById(organization.TimeZone);

            buffer = output.GetBuffer();
            return buffer;
        }
    }

    public class Header : PdfPageEventHelper
    {
        protected PdfContentByte canvas;
        protected Phrase header;

        public Header(Phrase header)
        {
            this.header = header;
        }

        public override void OnOpenDocument(PdfWriter writer, Document document)
        {
            try
            {
                canvas = writer.DirectContent;
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

            var pageNumber = new Phrase("End Page " + document.PageNumber.ToString());
            ColumnText.ShowTextAligned(canvas, Element.ALIGN_LEFT, pageNumber, 100, 100, 0);
            ColumnText.ShowTextAligned(canvas, Element.ALIGN_LEFT, header, 30, 30, 0);
        }

        public override void OnCloseDocument(PdfWriter writer, Document document)
        {
            base.OnCloseDocument(writer, document);

            var pageNumber = new Phrase("Close Page " + document.PageNumber.ToString());
            ColumnText.ShowTextAligned(canvas, Element.ALIGN_LEFT, pageNumber, 60, 60, 0);
        }
    }

    public class ITextEvents : PdfPageEventHelper
    {
        // This is the contentbyte object of the writer
        PdfContentByte cb;

        // we will put the final number of pages in a template
        PdfTemplate headerTemplate, footerTemplate;

        // this is the BaseFont we are going to use for the header / footer
        BaseFont bf = null;
        
        public string Header { get; set; }
        public DateTime PrintTime { get; set; }

        public ITextEvents(string header, DateTime printTime)
        {
            Header = header;
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
            
            PdfPCell headerCell1 = new PdfPCell(new Paragraph(Header, baseFontNormal));
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