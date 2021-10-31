using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Brizbee.Web.Services.Reports
{
    public static class Stylesheets
    {
        public static Stylesheet Common()
        {
            return new Stylesheet(
                new Fonts(

                    // Index 0 - Default font
                    new Font(
                        new FontSize() { Val = 9 },
                        new Color() { Rgb = new HexBinaryValue() { Value = "00000000" } },
                        new FontName() { Val = "Arial" }),

                    // Index 1 - Bold font
                    new Font(
                        new Bold(),
                        new FontSize() { Val = 9 },
                        new Color() { Rgb = new HexBinaryValue() { Value = "00000000" } },
                        new FontName() { Val = "Arial" }),

                    // Index 2 - Italic font
                    new Font(
                        new Italic(),
                        new FontSize() { Val = 9 },
                        new Color() { Rgb = new HexBinaryValue() { Value = "00000000" } },
                        new FontName() { Val = "Arial" }),

                    // Index 3 - White Bold Font
                    new Font(
                        new Bold(),
                        new FontSize() { Val = 9 },
                        new Color() { Rgb = new HexBinaryValue() { Value = "FFFFFFFF" } },
                        new FontName() { Val = "Arial" })
                ),
                new Fills(

                    // Index 0 - Dfault fill
                    new Fill(
                        new PatternFill() { PatternType = PatternValues.None }),

                    // Index 1 - The default fill of gray 125 (required)
                    new Fill(
                        new PatternFill() { PatternType = PatternValues.Gray125 }),

                    // Index 2 - The yellow fill
                    new Fill(
                        new PatternFill(
                            new ForegroundColor() { Rgb = new HexBinaryValue() { Value = "FFFFFF00" } }
                        )
                        { PatternType = PatternValues.Solid }),

                    // Index 3
                    new Fill(
                        new PatternFill(
                            new ForegroundColor() { Rgb = new HexBinaryValue() { Value = "00000000" } }
                        )
                        { PatternType = PatternValues.Solid })
                ),
                new Borders(

                    // Index 0 - Default border
                    new Border(
                        new LeftBorder(),
                        new RightBorder(),
                        new TopBorder(),
                        new BottomBorder(),
                        new DiagonalBorder()),

                    // Index 1 - Applies a Left, Right, Top, Bottom border to a cell
                    new Border(
                        new LeftBorder(
                            new Color() { Auto = true }
                        )
                        { Style = BorderStyleValues.Thin },
                        new RightBorder(
                            new Color() { Auto = true }
                        )
                        { Style = BorderStyleValues.Thin },
                        new TopBorder(
                            new Color() { Auto = true }
                        )
                        { Style = BorderStyleValues.Thin },
                        new BottomBorder(
                            new Color() { Auto = true }
                        )
                        { Style = BorderStyleValues.Thin },
                        new DiagonalBorder())
                ),
                new CellFormats(
                    // Index 0 - Default cell style
                    new CellFormat()
                    {
                        FontId = 0,
                        FillId = 0,
                        BorderId = 0,
                        ApplyFont = true,
                        Alignment = new Alignment()
                        {
                            Horizontal = HorizontalAlignmentValues.Left,
                            Vertical = VerticalAlignmentValues.Center
                        }
                    },

                    // Index 1 - Bold Left Align
                    new CellFormat()
                    {
                        FontId = 1,
                        FillId = 0,
                        BorderId = 0,
                        ApplyFont = true,
                        Alignment = new Alignment()
                        {
                            Horizontal = HorizontalAlignmentValues.Left,
                            Vertical = VerticalAlignmentValues.Center
                        }
                    },

                    // Index 2 - Bold Right Align
                    new CellFormat()
                    {
                        FontId = 1,
                        FillId = 0,
                        BorderId = 0,
                        ApplyFont = true,
                        Alignment = new Alignment()
                        {
                            Horizontal = HorizontalAlignmentValues.Right,
                            Vertical = VerticalAlignmentValues.Center
                        }
                    },

                    // Index 3 - Align Center
                    new CellFormat()
                    {
                        FontId = 0,
                        FillId = 0,
                        BorderId = 0,
                        ApplyFont = true,
                        Alignment = new Alignment()
                        {
                            Horizontal = HorizontalAlignmentValues.Center,
                            Vertical = VerticalAlignmentValues.Center
                        }
                    },

                    // Index 4 - Black Header
                    new CellFormat()
                    {
                        FontId = 3,
                        FillId = 3,
                        BorderId = 0,
                        ApplyFill = true,
                        Alignment = new Alignment()
                        {
                            Horizontal = HorizontalAlignmentValues.Left,
                            Vertical = VerticalAlignmentValues.Center
                        }
                    },

                    // Index 5 - Right Align
                    new CellFormat()
                    {
                        FontId = 0,
                        FillId = 0,
                        BorderId = 0,
                        ApplyFont = true,
                        Alignment = new Alignment()
                        {
                            Horizontal = HorizontalAlignmentValues.Right,
                            Vertical = VerticalAlignmentValues.Center
                        }
                    },

                    // Index 6 - Left Align
                    new CellFormat()
                    {
                        FontId = 0,
                        FillId = 0,
                        BorderId = 0,
                        ApplyFont = true,
                        Alignment = new Alignment()
                        {
                            Horizontal = HorizontalAlignmentValues.Left,
                            Vertical = VerticalAlignmentValues.Center
                        }
                    }
                )
            );
        }
    }
}