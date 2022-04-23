//
//  ReportBuilder.cs
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
using iText.Barcodes;
using iText.IO.Font.Constants;
using iText.Kernel.Events;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Brizbee.Web.Services
{
    public class ReportBuilder
    {
        private SqlContext db = new SqlContext();
        private PdfFont fontH1 = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
        private PdfFont fontP = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
        private PdfFont fontSubtitle = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

        //private PdfFont fontH1 = new Font(Font.FontFamily.HELVETICA, 9, Font.BOLD, BaseColor.WHITE);
        //private PdfFont fontH3 = new Font(Font.FontFamily.HELVETICA, 8, Font.BOLD);
        //private PdfFont fontH4 = new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD);
        //private PdfFont fontP = new Font(Font.FontFamily.HELVETICA, 8, Font.NORMAL);
        //private PdfFont fontFooter = new Font(Font.FontFamily.HELVETICA, 9, Font.NORMAL);
        //private float pageWidth = 0f;

        public byte[] TasksByProjectAsPdf(int projectId, User currentUser, string taskGroupScope)
        {
            var buffer = new byte[0];
            var stream = new MemoryStream();
            var timeZone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(currentUser.TimeZone);
            var nowInstant = SystemClock.Instance.GetCurrentInstant();
            var nowLocal = nowInstant.InZone(timeZone);
            var nowDateTime = nowLocal.LocalDateTime.ToDateTimeUnspecified();
            var organization = db.Organizations.Find(currentUser.OrganizationId);

            var project = db.Jobs
                .Where(p => p.Id == projectId)
                .FirstOrDefault();

            var writer = new PdfWriter(stream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf);

            // Define document properties.
            var info = pdf.GetDocumentInfo();
            info.SetTitle($"Barcodes for Project {project.Number} - {project.Name}");
            info.SetAuthor(currentUser.Name);
            info.SetCreator("BRIZBEE");

            // Page formatting.
            document.SetMargins(30, 30, 30, 30);

            // Set page header and footer.
            pdf.AddEventHandler(PdfDocumentEvent.END_PAGE, new MyEventHandler(organization.Name));

            var generatedParagraph = new Paragraph($"GENERATED {nowDateTime.ToString("ddd, MMM d, yyyy h:mm:ss tt").ToUpper()}");
            generatedParagraph.SetFont(fontP);
            generatedParagraph.SetFontSize(8);
            generatedParagraph.SetHorizontalAlignment(HorizontalAlignment.RIGHT);
            generatedParagraph.SetTextAlignment(TextAlignment.RIGHT);
            generatedParagraph.SetPaddingBottom(5);
            document.Add(generatedParagraph);

            var table = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 }))
                .UseAllAvailableWidth();

            // Project details are at the top of the table.
            var detailsCell = new Cell(0, 2);
            detailsCell.SetPadding(20);

            var customerParagraph = new Paragraph();
            customerParagraph.SetFont(fontH1);
            customerParagraph.SetPaddingBottom(0);

            if (organization.ShowCustomerNumber)
            {
                customerParagraph.Add($"CUSTOMER: {project.Customer.Number} - {project.Customer.Name.ToUpper()}");
            }
            else
            {
                customerParagraph.Add($"CUSTOMER: {project.Customer.Name.ToUpper()}");
            }

            var projectParagraph = new Paragraph();
            projectParagraph.SetFont(fontH1);
            projectParagraph.SetPaddingBottom(10);

            if (organization.ShowProjectNumber)
            {
                projectParagraph.Add($"PROJECT: {project.Number} - {project.Name.ToUpper()}");
            }
            else
            {
                projectParagraph.Add($"PROJECT: {project.Name.ToUpper()}");
            }
            
            var descriptionParagraph = new Paragraph($"DESCRIPTION: {project.Description}");
            descriptionParagraph.SetFont(fontP);
            descriptionParagraph.SetFontSize(9);

            detailsCell.Add(customerParagraph);
            detailsCell.Add(projectParagraph);
            detailsCell.Add(descriptionParagraph);
            table.AddCell(detailsCell);

            var tasks = new List<Task>(0);

            if (string.IsNullOrEmpty(taskGroupScope) || taskGroupScope == "Unspecified")
            {
                tasks = db.Tasks
                    .Where(t => t.JobId == projectId)
                    .ToList();
            }
            else
            {
                tasks = db.Tasks
                    .Where(t => t.JobId == projectId)
                    .Where(t => t.Group == taskGroupScope)
                    .ToList();
            }

            foreach (var task in tasks)
            {
                try
                {
                    // Generate a barcode.
                    Barcode128 barCode = new Barcode128(pdf);
                    barCode.SetTextAlignment(Barcode128.ALIGN_CENTER);
                    barCode.SetCode(task.Number);
                    barCode.SetStartStopText(false);
                    barCode.SetCodeType(Barcode128.CODE128);
                    barCode.SetExtended(true);
                    barCode.SetAltText("");
                    barCode.SetBarHeight(50);

                    var barCodeCell = new Cell();
                    barCodeCell.SetPadding(20);
                    barCodeCell.SetHorizontalAlignment(HorizontalAlignment.CENTER);

                    var barCodeParagraph = new Paragraph();
                    barCodeParagraph.SetTextAlignment(TextAlignment.CENTER);
                    barCodeParagraph.Add(new Image(barCode.CreateFormXObject(pdf)));

                    barCodeCell.Add(barCodeParagraph);

                    var subtitleParagraph = new Paragraph();
                    subtitleParagraph.SetFont(fontSubtitle);
                    subtitleParagraph.SetTextAlignment(TextAlignment.CENTER);
                    subtitleParagraph.Add($"{task.Number} - {task.Name.ToUpper()}");

                    barCodeCell.Add(subtitleParagraph);

                    table.AddCell(barCodeCell);
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }
            }

            if (tasks.Count % 2 != 0)
            {
                // Add a blank cell to balance the columns.
                var blankCell = new Cell();
                table.AddCell(blankCell);
            }

            document.Add(table);

            for (int i = 0; i <= pdf.GetNumberOfPages(); i++)
            {
            }

            document.Close();

            buffer = stream.ToArray();

            return buffer;
        }

        protected internal class MyEventHandler : IEventHandler
        {
            public string OrganizationName { get; set; }

            public MyEventHandler(string organizationName)
            {
                OrganizationName = organizationName;
            }

            public virtual void HandleEvent(Event @event)
            {
                PdfDocumentEvent docEvent = (PdfDocumentEvent)@event;
                PdfDocument pdfDoc = docEvent.GetDocument();
                PdfPage page = docEvent.GetPage();

                int pageNumber = pdfDoc.GetPageNumber(page);
                Rectangle pageSize = page.GetPageSize();
                PdfCanvas pdfCanvas = new PdfCanvas(page.NewContentStreamBefore(), page.GetResources(), pdfDoc);

                // Add header and footer
                pdfCanvas.BeginText()
                    .SetFontAndSize(PdfFontFactory.CreateFont(StandardFonts.HELVETICA), 9)
                    .MoveText(pageSize.GetWidth() / 2 - 60, pageSize.GetTop() - 20)
                    //.ShowText(pageNumber.ToString())
                    .MoveText(0, -pageSize.GetTop() + 50)
                    .ShowText(OrganizationName.ToUpper())
                    .EndText();

                pdfCanvas.Release();
            }
        }
    }

    //public class Header : PdfPageEventHelper
    //{
    //    // This is the contentbyte object of the writer
    //    PdfContentByte cb;

    //    // we will put the final number of pages in a template
    //    PdfTemplate headerTemplate, footerTemplate;

    //    // this is the BaseFont we are going to use for the header / footer
    //    BaseFont bf = null;

    //    public string Title { get; set; }
    //    public DateTime PrintTime { get; set; }
    //    public string OrganizationName { get; set; }

    //    public Header(string title, DateTime printTime, string organizationName)
    //    {
    //        Title = title;
    //        PrintTime = printTime;
    //        OrganizationName = organizationName;
    //    }

    //    public override void OnOpenDocument(PdfWriter writer, Document document)
    //    {
    //        try
    //        {
    //            bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
    //            cb = writer.DirectContent;

    //            var width = document.PageSize.Width - document.LeftMargin - document.RightMargin;

    //            headerTemplate = cb.CreateTemplate(width, 30);
    //            footerTemplate = cb.CreateTemplate(width, 30);
    //        }
    //        catch (DocumentException ex)
    //        {
    //            Trace.TraceError(ex.ToString());
    //        }
    //        catch (IOException ex)
    //        {
    //            Trace.TraceError(ex.ToString());
    //        }
    //    }

    //    public override void OnEndPage(PdfWriter writer, Document document)
    //    {
    //        base.OnEndPage(writer, document);

    //        Font baseFontNormal = new Font(Font.FontFamily.HELVETICA, 9, Font.NORMAL, BaseColor.BLACK);
    //        Font baseFontBig = new Font(Font.FontFamily.HELVETICA, 9, Font.BOLD, BaseColor.BLACK);

    //        PdfPTable tableHeader = new PdfPTable(2);

    //        PdfPCell headerCell1 = new PdfPCell(new Paragraph(Title, baseFontNormal));
    //        PdfPCell headerCell2 = new PdfPCell(new Paragraph(string.Format("Generated {0}", PrintTime.ToString("ddd, MMM d, yyyy h:mm:ss tt")).ToUpper(), baseFontNormal));

    //        // Set cell styling
    //        headerCell1.HorizontalAlignment = Element.ALIGN_LEFT;
    //        headerCell2.HorizontalAlignment = Element.ALIGN_RIGHT;
    //        headerCell1.VerticalAlignment = Element.ALIGN_MIDDLE;
    //        headerCell2.VerticalAlignment = Element.ALIGN_MIDDLE;
    //        headerCell1.Border = 0;
    //        headerCell2.Border = 0;

    //        // Add cells to table
    //        tableHeader.AddCell(headerCell1);
    //        tableHeader.AddCell(headerCell2);

    //        // Table is full width
    //        tableHeader.TotalWidth = document.PageSize.Width - document.LeftMargin - document.RightMargin;

    //        //call WriteSelectedRows of PdfTable. This writes rows from PdfWriter in PdfTable
    //        //first param is start row. -1 indicates there is no end row and all the rows to be included to write
    //        //Third and fourth param is x and y position to start writing
    //        //table.WriteSelectedRows(0, -1, 40, document.PageSize.Height - 30, writer.DirectContent);
    //        tableHeader.WriteSelectedRows(0, -1, document.LeftMargin, document.PageSize.Height - 30, writer.DirectContent);


    //        PdfPTable tableFooter = new PdfPTable(2);

    //        tableFooter.WidthPercentage = 100;
    //        tableFooter.SetWidths(new float[] { 20, 80 });

    //        // Gather logo and configure scaling.
    //        string contentPath = HostingEnvironment.MapPath("~/Content");
    //        Image logo = Image.GetInstance(contentPath + "/logo.png");
    //        logo.ScaleAbsolute(61f, 10f);

    //        PdfPCell footerCell1 = new PdfPCell(logo);

    //        // Set cell styling
    //        footerCell1.HorizontalAlignment = Element.ALIGN_LEFT;
    //        footerCell1.VerticalAlignment = Element.ALIGN_MIDDLE;
    //        footerCell1.Border = 0;

    //        // Add cells to table
    //        tableFooter.AddCell(footerCell1);

    //        PdfPCell footerCell2 = new PdfPCell(new Paragraph(OrganizationName.ToUpper(), baseFontNormal));

    //        // Set cell styling
    //        footerCell2.HorizontalAlignment = Element.ALIGN_RIGHT;
    //        footerCell2.VerticalAlignment = Element.ALIGN_MIDDLE;
    //        footerCell2.Border = 0;

    //        // Add cells to table
    //        tableFooter.AddCell(footerCell2);

    //        // Table is full width
    //        tableFooter.TotalWidth = document.PageSize.Width - document.LeftMargin - document.RightMargin;

    //        tableFooter.WriteSelectedRows(0, -1, document.LeftMargin, document.PageSize.GetBottom(38), writer.DirectContent);

    //        //Move the pointer and draw line to separate header section from rest of page
    //        cb.MoveTo(document.LeftMargin, document.PageSize.Height - 50);
    //        cb.LineTo(document.PageSize.Width - document.RightMargin, document.PageSize.Height - 50);
    //        cb.Stroke();

    //        //Move the pointer and draw line to separate footer section from rest of page
    //        cb.MoveTo(document.LeftMargin, document.PageSize.GetBottom(50));
    //        cb.LineTo(document.PageSize.Width - document.RightMargin, document.PageSize.GetBottom(50));
    //        cb.Stroke();
    //    }
    //}
}
