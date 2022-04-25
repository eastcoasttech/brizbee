//
//  ReportBuilder.cs
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

using Brizbee.Api;
using Brizbee.Core.Models;
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
using Microsoft.EntityFrameworkCore;
using NodaTime;
using System.Diagnostics;

namespace Brizbee.Api.Services
{
    public class ReportBuilder
    {
        private PdfFont fontH1 = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
        private PdfFont fontP = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
        private PdfFont fontSubtitle = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

        public byte[] TasksByProjectAsPdf(SqlContext context, int projectId, User currentUser, string taskGroupScope)
        {
            var buffer = new byte[0];
            var stream = new MemoryStream();
            var timeZone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(currentUser.TimeZone);
            var nowInstant = SystemClock.Instance.GetCurrentInstant();
            var nowLocal = nowInstant.InZone(timeZone);
            var nowDateTime = nowLocal.LocalDateTime.ToDateTimeUnspecified();
            var organization = context.Organizations.Find(currentUser.OrganizationId);

            var project = context.Jobs
                .Include("Customer")
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

            var tasks = new List<Brizbee.Core.Models.Task>(0);

            if (string.IsNullOrEmpty(taskGroupScope) || taskGroupScope == "Unspecified")
            {
                tasks = context.Tasks
                    .Where(t => t.JobId == projectId)
                    .ToList();
            }
            else
            {
                tasks = context.Tasks
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
                    .MoveText(0, -pageSize.GetTop() + 50)
                    .ShowText(OrganizationName.ToUpper())
                    .EndText();

                pdfCanvas.Release();
            }
        }
    }
}
