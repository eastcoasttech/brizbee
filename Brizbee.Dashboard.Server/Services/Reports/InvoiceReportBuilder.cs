using Brizbee.Core.Models;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.EntityFrameworkCore;

namespace Brizbee.Dashboard.Server.Services.Reports;

public class InvoiceReportBuilder
{
    private readonly PrimaryContext _context;

    public InvoiceReportBuilder(PrimaryContext context)
    {
        _context = context;
    }

    public async Task<byte[]> InvoiceAsPdfAsync(long invoiceId, User currentUser)
    {
        var fontParagraph = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

        var stream = new MemoryStream();

        var invoice = await _context.Invoices!
            .Include(i => i.Customer)
            .FirstAsync(i => i.Id == invoiceId);

        var writer = new PdfWriter(stream);
        var pdf = new PdfDocument(writer);
        var document = new Document(pdf);

        // Define document properties.
        var info = pdf.GetDocumentInfo();
        info.SetTitle($"Invoice {invoice.Number}");
        info.SetAuthor(currentUser.Name);
        info.SetCreator("BRIZBEE");

        // Page formatting.
        document.SetMargins(50, 50, 50, 50);
        

        // --------------------------------------------------------------------
        // Header Table
        // --------------------------------------------------------------------

        var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 70, 30 }))
            .UseAllAvailableWidth();
        headerTable.SetBorder(null);
        headerTable.SetPadding(0);
        

        // --------------------------------------------------------------------
        // Company and Address
        // --------------------------------------------------------------------

        var companyCell = new Cell();
        companyCell.SetBorder(null);

        var companyNameParagraph = new Paragraph("EAST COAST TECHNOLOGY SERVICES LLC");
        companyNameParagraph.SetFont(fontParagraph);
        companyNameParagraph.SetFontSize(11);
        companyNameParagraph.SetHorizontalAlignment(HorizontalAlignment.LEFT);
        companyNameParagraph.SetTextAlignment(TextAlignment.LEFT);
        companyCell.Add(companyNameParagraph);
        
        var companyAddressParagraph = new Paragraph("428 WALLER LN");
        companyAddressParagraph.SetFont(fontParagraph);
        companyAddressParagraph.SetFontSize(10);
        companyAddressParagraph.SetHorizontalAlignment(HorizontalAlignment.LEFT);
        companyAddressParagraph.SetTextAlignment(TextAlignment.LEFT);
        companyCell.Add(companyAddressParagraph);
        
        var companyCityStateZipParagraph = new Paragraph("STUART, VA 24171");
        companyCityStateZipParagraph.SetFont(fontParagraph);
        companyCityStateZipParagraph.SetFontSize(10);
        companyCityStateZipParagraph.SetHorizontalAlignment(HorizontalAlignment.LEFT);
        companyCityStateZipParagraph.SetTextAlignment(TextAlignment.LEFT);
        companyCell.Add(companyCityStateZipParagraph);
        
        headerTable.AddCell(companyCell);


        // --------------------------------------------------------------------
        // Title
        // --------------------------------------------------------------------

        var titleCell = new Cell();
        titleCell.SetBorder(null);
        
        var titleParagraph = new Paragraph("INVOICE");
        titleParagraph.SetFont(fontParagraph);
        titleParagraph.SetFontSize(20);
        titleParagraph.SetHorizontalAlignment(HorizontalAlignment.RIGHT);
        titleParagraph.SetTextAlignment(TextAlignment.RIGHT);
        titleCell.Add(titleParagraph);
        
        headerTable.AddCell(titleCell);

        // Blank Cell
        var blankCell1 = new Cell();
        blankCell1.SetBorder(null);
        headerTable.AddCell(blankCell1);
        

        // --------------------------------------------------------------------
        // Details Table
        // --------------------------------------------------------------------

        var detailsTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 }))
            .UseAllAvailableWidth();
        detailsTable.SetBorder(null);
        detailsTable.SetPadding(0);

        // Date Header
        var dateHeaderCell = new Cell();
        dateHeaderCell.SetHeight(20);
        dateHeaderCell.SetVerticalAlignment(VerticalAlignment.MIDDLE);
        dateHeaderCell.SetPadding(3);
        var dateHeaderParagraph = new Paragraph("DATE");
        dateHeaderParagraph.SetFont(fontParagraph);
        dateHeaderParagraph.SetFontSize(10);
        dateHeaderParagraph.SetBold();
        dateHeaderParagraph.SetHorizontalAlignment(HorizontalAlignment.RIGHT);
        dateHeaderParagraph.SetTextAlignment(TextAlignment.RIGHT);
        dateHeaderCell.Add(dateHeaderParagraph);
        
        detailsTable.AddCell(dateHeaderCell);

        // Date
        var dateCell = new Cell();
        dateCell.SetHeight(20);
        dateCell.SetVerticalAlignment(VerticalAlignment.MIDDLE);
        dateCell.SetPadding(3);
        var dateParagraph = new Paragraph(invoice.EnteredOn.ToShortDateString());
        dateParagraph.SetFont(fontParagraph);
        dateParagraph.SetFontSize(10);
        dateParagraph.SetHorizontalAlignment(HorizontalAlignment.RIGHT);
        dateParagraph.SetTextAlignment(TextAlignment.RIGHT);
        dateCell.Add(dateParagraph);

        detailsTable.AddCell(dateCell);
        
        // Number Header
        var numberHeaderCell = new Cell();
        numberHeaderCell.SetHeight(20);
        numberHeaderCell.SetVerticalAlignment(VerticalAlignment.MIDDLE);
        numberHeaderCell.SetPadding(3);
        var numberHeaderParagraph = new Paragraph("NUMBER");
        numberHeaderParagraph.SetFont(fontParagraph);
        numberHeaderParagraph.SetFontSize(10);
        numberHeaderParagraph.SetBold();
        numberHeaderParagraph.SetHorizontalAlignment(HorizontalAlignment.RIGHT);
        numberHeaderParagraph.SetTextAlignment(TextAlignment.RIGHT);
        numberHeaderCell.Add(numberHeaderParagraph);
        
        detailsTable.AddCell(numberHeaderCell);

        // Number
        var numberCell = new Cell();
        numberCell.SetHeight(20);
        numberCell.SetVerticalAlignment(VerticalAlignment.MIDDLE);
        numberCell.SetPadding(3);
        var numberParagraph = new Paragraph(invoice.Number);
        numberParagraph.SetFont(fontParagraph);
        numberParagraph.SetFontSize(10);
        numberParagraph.SetHorizontalAlignment(HorizontalAlignment.RIGHT);
        numberParagraph.SetTextAlignment(TextAlignment.RIGHT);
        numberCell.Add(numberParagraph);

        detailsTable.AddCell(numberCell);
        
        // Due Header
        var dueHeaderCell = new Cell();
        dueHeaderCell.SetHeight(20);
        dueHeaderCell.SetVerticalAlignment(VerticalAlignment.MIDDLE);
        dueHeaderCell.SetPadding(3);
        var dueHeaderParagraph = new Paragraph("DUE");
        dueHeaderParagraph.SetFont(fontParagraph);
        dueHeaderParagraph.SetFontSize(10);
        dueHeaderParagraph.SetBold();
        dueHeaderParagraph.SetHorizontalAlignment(HorizontalAlignment.RIGHT);
        dueHeaderParagraph.SetTextAlignment(TextAlignment.RIGHT);
        dueHeaderCell.Add(dueHeaderParagraph);
        
        detailsTable.AddCell(dueHeaderCell);

        // Due
        var dueCell = new Cell();
        dueCell.SetHeight(20);
        dueCell.SetVerticalAlignment(VerticalAlignment.MIDDLE);
        dueCell.SetPadding(3);
        var dueParagraph = new Paragraph(invoice.DueOn.ToShortDateString());
        dueParagraph.SetFont(fontParagraph);
        dueParagraph.SetFontSize(10);
        dueParagraph.SetHorizontalAlignment(HorizontalAlignment.RIGHT);
        dueParagraph.SetTextAlignment(TextAlignment.RIGHT);
        dueCell.Add(dueParagraph);

        detailsTable.AddCell(dueCell);

        var detailsCell = new Cell();
        detailsCell.SetBorder(null);
        detailsCell.SetPadding(0);

        detailsCell.Add(detailsTable);

        headerTable.AddCell(detailsCell);
        

        // --------------------------------------------------------------------
        // Customer
        // --------------------------------------------------------------------

        var customerCell = new Cell();
        customerCell.SetBorder(null);
        customerCell.SetPaddingTop(18);

        var customerNameParagraph = new Paragraph(invoice.Customer!.Name!.ToUpper());
        customerNameParagraph.SetFont(fontParagraph);
        customerNameParagraph.SetFontSize(10);
        customerNameParagraph.SetHorizontalAlignment(HorizontalAlignment.LEFT);
        customerNameParagraph.SetTextAlignment(TextAlignment.LEFT);
        customerCell.Add(customerNameParagraph);
        
        var customerAddressParagraph = new Paragraph("30-30 47TH AVE, SUITE 540");
        customerAddressParagraph.SetFont(fontParagraph);
        customerAddressParagraph.SetFontSize(10);
        customerAddressParagraph.SetHorizontalAlignment(HorizontalAlignment.LEFT);
        customerAddressParagraph.SetTextAlignment(TextAlignment.LEFT);
        customerCell.Add(customerAddressParagraph);
        
        var customerCityStateZipParagraph = new Paragraph("LONG ISLAND CITY, NY 11101");
        customerCityStateZipParagraph.SetFont(fontParagraph);
        customerCityStateZipParagraph.SetFontSize(10);
        customerCityStateZipParagraph.SetHorizontalAlignment(HorizontalAlignment.LEFT);
        customerCityStateZipParagraph.SetTextAlignment(TextAlignment.LEFT);
        customerCell.Add(customerCityStateZipParagraph);

        headerTable.AddCell(customerCell);
        
        // Blank Cell
        var blankCell2 = new Cell();
        blankCell2.SetBorder(null);
        headerTable.AddCell(blankCell2);
        
        document.Add(headerTable);
        

        // --------------------------------------------------------------------
        // Line Items
        // --------------------------------------------------------------------

        var lineItemsTable = new Table(UnitValue.CreatePercentArray(new float[] { 60, 10, 15, 15 }))
            .UseAllAvailableWidth();
        lineItemsTable.SetBorder(null);
        lineItemsTable.SetPadding(0);
        lineItemsTable.SetMarginTop(30);

        // Description Header
        var descriptionHeaderCell = new Cell();
        descriptionHeaderCell.SetHeight(20);
        descriptionHeaderCell.SetVerticalAlignment(VerticalAlignment.MIDDLE);
        descriptionHeaderCell.SetPadding(3);
        var descriptionHeaderParagraph = new Paragraph("DESCRIPTION");
        descriptionHeaderParagraph.SetFont(fontParagraph);
        descriptionHeaderParagraph.SetFontSize(10);
        descriptionHeaderParagraph.SetBold();
        descriptionHeaderParagraph.SetHorizontalAlignment(HorizontalAlignment.LEFT);
        descriptionHeaderParagraph.SetTextAlignment(TextAlignment.LEFT);
        descriptionHeaderCell.Add(descriptionHeaderParagraph);

        lineItemsTable.AddCell(descriptionHeaderCell);

        // Quantity Header
        var quantityHeaderCell = new Cell();
        quantityHeaderCell.SetHeight(20);
        quantityHeaderCell.SetVerticalAlignment(VerticalAlignment.MIDDLE);
        quantityHeaderCell.SetPadding(3);
        var quantityHeaderParagraph = new Paragraph("QTY");
        quantityHeaderParagraph.SetFont(fontParagraph);
        quantityHeaderParagraph.SetFontSize(10);
        quantityHeaderParagraph.SetBold();
        quantityHeaderParagraph.SetHorizontalAlignment(HorizontalAlignment.LEFT);
        quantityHeaderParagraph.SetTextAlignment(TextAlignment.LEFT);
        quantityHeaderCell.Add(quantityHeaderParagraph);

        lineItemsTable.AddCell(quantityHeaderCell);

        // Unit Amount Header
        var unitAmountHeaderCell = new Cell();
        unitAmountHeaderCell.SetHeight(20);
        unitAmountHeaderCell.SetVerticalAlignment(VerticalAlignment.MIDDLE);
        unitAmountHeaderCell.SetPadding(3);
        var unitAmountHeaderParagraph = new Paragraph("UNIT");
        unitAmountHeaderParagraph.SetFont(fontParagraph);
        unitAmountHeaderParagraph.SetFontSize(10);
        unitAmountHeaderParagraph.SetBold();
        unitAmountHeaderParagraph.SetHorizontalAlignment(HorizontalAlignment.LEFT);
        unitAmountHeaderParagraph.SetTextAlignment(TextAlignment.LEFT);
        unitAmountHeaderCell.Add(unitAmountHeaderParagraph);

        lineItemsTable.AddCell(unitAmountHeaderCell);

        // Subtotal Header
        var subtotalHeaderCell = new Cell();
        subtotalHeaderCell.SetHeight(20);
        subtotalHeaderCell.SetVerticalAlignment(VerticalAlignment.MIDDLE);
        subtotalHeaderCell.SetPadding(3);
        var subtotalHeaderParagraph = new Paragraph("SUBTOTAL");
        subtotalHeaderParagraph.SetFont(fontParagraph);
        subtotalHeaderParagraph.SetFontSize(10);
        subtotalHeaderParagraph.SetBold();
        subtotalHeaderParagraph.SetHorizontalAlignment(HorizontalAlignment.LEFT);
        subtotalHeaderParagraph.SetTextAlignment(TextAlignment.LEFT);
        subtotalHeaderCell.Add(subtotalHeaderParagraph);

        lineItemsTable.AddCell(subtotalHeaderCell);

        var lineItems = await _context.LineItems!
            .Where(l => l.InvoiceId == invoiceId)
            .ToListAsync();

        foreach (var lineItem in lineItems)
        {
            // Description
            var descriptionCell = new Cell();
            descriptionCell.SetHeight(20);
            descriptionCell.SetVerticalAlignment(VerticalAlignment.MIDDLE);
            descriptionCell.SetPadding(3);
            var descriptionParagraph = new Paragraph(lineItem.Description);
            descriptionParagraph.SetFont(fontParagraph);
            descriptionParagraph.SetFontSize(10);
            descriptionParagraph.SetHorizontalAlignment(HorizontalAlignment.LEFT);
            descriptionParagraph.SetTextAlignment(TextAlignment.LEFT);
            descriptionCell.Add(descriptionParagraph);

            lineItemsTable.AddCell(descriptionCell);
            
            // Quantity
            var quantityCell = new Cell();
            quantityCell.SetHeight(20);
            quantityCell.SetVerticalAlignment(VerticalAlignment.MIDDLE);
            quantityCell.SetPadding(3);
            var quantityParagraph = new Paragraph(lineItem.Quantity.ToString("N2"));
            quantityParagraph.SetFont(fontParagraph);
            quantityParagraph.SetFontSize(10);
            quantityParagraph.SetHorizontalAlignment(HorizontalAlignment.LEFT);
            quantityParagraph.SetTextAlignment(TextAlignment.LEFT);
            quantityCell.Add(quantityParagraph);

            lineItemsTable.AddCell(quantityCell);
            
            // Unit Amount
            var unitAmountCell = new Cell();
            unitAmountCell.SetHeight(20);
            unitAmountCell.SetVerticalAlignment(VerticalAlignment.MIDDLE);
            unitAmountCell.SetPadding(3);
            var unitAmountParagraph = new Paragraph(lineItem.UnitAmount.ToString("C2"));
            unitAmountParagraph.SetFont(fontParagraph);
            unitAmountParagraph.SetFontSize(10);
            unitAmountParagraph.SetHorizontalAlignment(HorizontalAlignment.LEFT);
            unitAmountParagraph.SetTextAlignment(TextAlignment.LEFT);
            unitAmountCell.Add(unitAmountParagraph);

            lineItemsTable.AddCell(unitAmountCell);
            
            // Subtotal
            var subtotalCell = new Cell();
            subtotalCell.SetHeight(20);
            subtotalCell.SetVerticalAlignment(VerticalAlignment.MIDDLE);
            subtotalCell.SetPadding(3);
            var subtotalParagraph = new Paragraph((lineItem.UnitAmount * lineItem.Quantity).ToString("C2"));
            subtotalParagraph.SetFont(fontParagraph);
            subtotalParagraph.SetFontSize(10);
            subtotalParagraph.SetHorizontalAlignment(HorizontalAlignment.LEFT);
            subtotalParagraph.SetTextAlignment(TextAlignment.LEFT);
            subtotalCell.Add(subtotalParagraph);

            lineItemsTable.AddCell(subtotalCell);
        }

        // Spacer Row
        var spacerCell = new Cell(0, 4);
        spacerCell.SetHeight(20);
        lineItemsTable.AddCell(spacerCell);

        // Invoice Subtotal Header
        var invoiceSubtotalHeaderCell = new Cell(0, 3);
        invoiceSubtotalHeaderCell.SetHeight(20);
        invoiceSubtotalHeaderCell.SetVerticalAlignment(VerticalAlignment.MIDDLE);
        invoiceSubtotalHeaderCell.SetPadding(3);
        var invoiceSubtotalHeaderParagraph = new Paragraph("SUBTOTAL");
        invoiceSubtotalHeaderParagraph.SetFont(fontParagraph);
        invoiceSubtotalHeaderParagraph.SetFontSize(10);
        invoiceSubtotalHeaderParagraph.SetBold();
        invoiceSubtotalHeaderParagraph.SetHorizontalAlignment(HorizontalAlignment.RIGHT);
        invoiceSubtotalHeaderParagraph.SetTextAlignment(TextAlignment.RIGHT);
        invoiceSubtotalHeaderCell.Add(invoiceSubtotalHeaderParagraph);

        lineItemsTable.AddCell(invoiceSubtotalHeaderCell);
        
        // Invoice Subtotal
        var invoiceSubtotalCell = new Cell();
        invoiceSubtotalCell.SetHeight(20);
        invoiceSubtotalCell.SetVerticalAlignment(VerticalAlignment.MIDDLE);
        invoiceSubtotalCell.SetPadding(3);
        var invoiceSubtotalParagraph = new Paragraph(lineItems.Sum(l => l.UnitAmount * l.Quantity).ToString("C2"));
        invoiceSubtotalParagraph.SetFont(fontParagraph);
        invoiceSubtotalParagraph.SetFontSize(10);
        invoiceSubtotalParagraph.SetHorizontalAlignment(HorizontalAlignment.LEFT);
        invoiceSubtotalParagraph.SetTextAlignment(TextAlignment.LEFT);
        invoiceSubtotalCell.Add(invoiceSubtotalParagraph);

        lineItemsTable.AddCell(invoiceSubtotalCell);
        
        // Invoice Total Header
        var totalHeaderCell = new Cell(0, 3);
        totalHeaderCell.SetHeight(20);
        totalHeaderCell.SetVerticalAlignment(VerticalAlignment.MIDDLE);
        totalHeaderCell.SetPadding(3);
        var totalHeaderParagraph = new Paragraph("TOTAL");
        totalHeaderParagraph.SetFont(fontParagraph);
        totalHeaderParagraph.SetFontSize(10);
        totalHeaderParagraph.SetBold();
        totalHeaderParagraph.SetHorizontalAlignment(HorizontalAlignment.RIGHT);
        totalHeaderParagraph.SetTextAlignment(TextAlignment.RIGHT);
        totalHeaderCell.Add(totalHeaderParagraph);

        lineItemsTable.AddCell(totalHeaderCell);

        // Invoice Total
        var totalCell = new Cell();
        totalCell.SetHeight(20);
        totalCell.SetVerticalAlignment(VerticalAlignment.MIDDLE);
        totalCell.SetPadding(3);
        var totalParagraph = new Paragraph(lineItems.Sum(l => l.UnitAmount * l.Quantity).ToString("C2"));
        totalParagraph.SetFont(fontParagraph);
        totalParagraph.SetFontSize(10);
        totalParagraph.SetHorizontalAlignment(HorizontalAlignment.LEFT);
        totalParagraph.SetTextAlignment(TextAlignment.LEFT);
        totalCell.Add(totalParagraph);

        lineItemsTable.AddCell(totalCell);

        document.Add(lineItemsTable);

        document.Flush();

        document.Close();

        var buffer = stream.ToArray();

        return buffer;
    }
}
