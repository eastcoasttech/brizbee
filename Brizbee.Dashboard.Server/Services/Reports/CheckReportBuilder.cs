using Brizbee.Core.Models;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.EntityFrameworkCore;

namespace Brizbee.Dashboard.Server.Services.Reports;

public class CheckReportBuilder
{
    private readonly PrimaryContext _context;

    public CheckReportBuilder(PrimaryContext context)
    {
        _context = context;
    }

    public async Task<byte[]> CheckAsPdfAsync(long checkId, User currentUser)
    {
        var fontParagraph = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

        var stream = new MemoryStream();

        var check = await _context.Checks!
            .Include(c => c.Vendor)
            .FirstAsync(c => c.Id == checkId);

        var writer = new PdfWriter(stream);
        var pdf = new PdfDocument(writer);
        var document = new Document(pdf);

        // Define document properties.
        var info = pdf.GetDocumentInfo();
        info.SetTitle($"Check {check.Number}");
        info.SetAuthor(currentUser.Name);
        info.SetCreator("BRIZBEE");

        // Page formatting.
        document.SetMargins(50, 25, 50, 30);

        var primaryTable = new Table(UnitValue.CreatePercentArray(new float[] { 80, 20 }))
            .UseAllAvailableWidth();
        primaryTable.SetBorder(null);
        primaryTable.SetPadding(0);

        var dateCell = new Cell(0, 2);
        dateCell.SetBorder(null);
        dateCell.SetPaddingTop(30);
        var dateParagraph = new Paragraph(check.EnteredOn.ToShortDateString());
        dateParagraph.SetFont(fontParagraph);
        dateParagraph.SetFontSize(10);
        dateParagraph.SetHorizontalAlignment(HorizontalAlignment.RIGHT);
        dateParagraph.SetTextAlignment(TextAlignment.RIGHT);
        dateCell.Add(dateParagraph);

        primaryTable.AddCell(dateCell);

        var vendorCell = new Cell();
        vendorCell.SetBorder(null);
        vendorCell.SetPaddingLeft(35);
        vendorCell.SetPaddingTop(18);
        var vendorParagraph = new Paragraph(check.Vendor!.Name!.ToUpper());
        vendorParagraph.SetFont(fontParagraph);
        vendorParagraph.SetFontSize(10);
        vendorParagraph.SetHorizontalAlignment(HorizontalAlignment.LEFT);
        vendorParagraph.SetTextAlignment(TextAlignment.LEFT);
        vendorCell.Add(vendorParagraph);

        primaryTable.AddCell(vendorCell);
        
        var amountCell = new Cell();
        amountCell.SetBorder(null);
        amountCell.SetPaddingTop(18);
        var amountParagraph = new Paragraph(check.TotalAmount.ToString("N2"));
        amountParagraph.SetFont(fontParagraph);
        amountParagraph.SetFontSize(10);
        amountParagraph.SetHorizontalAlignment(HorizontalAlignment.RIGHT);
        amountParagraph.SetTextAlignment(TextAlignment.RIGHT);
        amountCell.Add(amountParagraph);
        
        primaryTable.AddCell(amountCell);

        var verbalizedCell = new Cell();
        verbalizedCell.SetBorder(null);
        verbalizedCell.SetPaddingTop(8);
        var verbalizedParagraph = new Paragraph(ToVerbalCurrency(check.TotalAmount));
        verbalizedParagraph.SetFont(fontParagraph);
        verbalizedParagraph.SetFontSize(10);
        verbalizedParagraph.SetHorizontalAlignment(HorizontalAlignment.LEFT);
        verbalizedParagraph.SetTextAlignment(TextAlignment.LEFT);
        verbalizedCell.Add(verbalizedParagraph);
        
        primaryTable.AddCell(verbalizedCell);

        var blankCell = new Cell();
        blankCell.SetBorder(null);
        primaryTable.AddCell(blankCell);
        
        var memoCell = new Cell();
        memoCell.SetBorder(null);
        memoCell.SetPaddingLeft(30);
        memoCell.SetPaddingTop(60);
        var memoParagraph = new Paragraph(check.Memo.ToUpper());
        memoParagraph.SetFont(fontParagraph);
        memoParagraph.SetFontSize(10);
        memoParagraph.SetHorizontalAlignment(HorizontalAlignment.LEFT);
        memoParagraph.SetTextAlignment(TextAlignment.LEFT);
        memoCell.Add(memoParagraph);
        
        primaryTable.AddCell(memoCell);

        document.Add(primaryTable);

        // Details
        var table = new Table(UnitValue.CreatePercentArray(new float[] { 10, 70, 20 }))
            .UseAllAvailableWidth();

        document.Add(table);

        document.Flush();

        document.Close();

        var buffer = stream.ToArray();

        return buffer;
    }

    private static string ToVerbalCurrency(decimal value)
    {
        var valueString = value.ToString("N2");
        var decimalString = valueString[(valueString.LastIndexOf('.') + 1)..];
        var wholeString = valueString[..valueString.LastIndexOf('.')];

        var valueArray = wholeString.Split(',');

        var unitsMap = new[] { "", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
        var tensMap = new[] { "", "ten", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };
        var placeMap = new[] { "", " thousand ", " million ", " billion ", " trillion " };

        var outList = new List<string>();

        var placeIndex = 0;

        for (var i = valueArray.Length - 1; i >= 0; i--)
        {
            var intValue = int.Parse(valueArray[i]);
            var tensValue = intValue % 100;

            string tensString;
            if (tensValue < unitsMap.Length) tensString = unitsMap[tensValue];
            else tensString = tensMap[(tensValue - tensValue % 10) / 10] + " " + unitsMap[tensValue % 10];

            var fullValue = string.Empty;
            if (intValue >= 100) fullValue = unitsMap[(intValue - intValue % 100) / 100] + " hundred " + tensString + placeMap[placeIndex++];
            else if (intValue != 0) fullValue = tensString + placeMap[placeIndex++];
            else placeIndex++;

            outList.Add(fullValue);
        }

        var intCentsValue = int.Parse(decimalString);

        string centsString;
        if (intCentsValue < unitsMap.Length) centsString = unitsMap[intCentsValue];
        else centsString = tensMap[(intCentsValue - intCentsValue % 10) / 10] + " " + unitsMap[intCentsValue % 10];

        if (intCentsValue == 0) centsString = "zero";

        var output = string.Empty;
        for (var i = outList.Count - 1; i >= 0; i--) output += outList[i];
        output += " dollars and " + centsString + " cents";

        return output.ToUpper();
    }
}
