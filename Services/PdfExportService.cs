using System;
using System.Linq;
using System.Text;
using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SchemaDiagramViewer.Models;

namespace SchemaDiagramViewer.Services;

public class PdfExportService
{
    // documentName and printedDate optional; settings UI will supply values when calling
    public byte[] GeneratePdf(DatabaseSchema? schema, float widthInches, float heightInches, string documentName = "Data Model", DateTime? printedDate = null)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        if (schema == null)
            return Document.Create(c => { }).GeneratePdf();

        var widthPoints = Math.Max(1f, widthInches) * 72f;
        var heightPoints = Math.Max(1f, heightInches) * 72f;

        if ((schema.Tables == null || !schema.Tables.Any()) && (schema.Views == null || !schema.Views.Any()))
            return Document.Create(c => { }).GeneratePdf();

        // Compute bounds
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        void Consider(dynamic item)
        {
            float boxW = 200f;
            float boxH = 30f + ((item.Columns?.Count ?? 0) * 16f);
            minX = Math.Min(minX, (float)item.X);
            minY = Math.Min(minY, (float)item.Y);
            maxX = Math.Max(maxX, (float)(item.X + boxW));
            maxY = Math.Max(maxY, (float)(item.Y + boxH));
        }
        if (schema.Tables != null) foreach (var t in schema.Tables) Consider(t);
        if (schema.Views != null) foreach (var v in schema.Views) Consider(v);
        if (schema.Shapes != null) 
        {
            foreach (var s in schema.Shapes) 
            {
                minX = Math.Min(minX, (float)s.X);
                minY = Math.Min(minY, (float)s.Y);
                maxX = Math.Max(maxX, (float)(s.X + s.Width));
                maxY = Math.Max(maxY, (float)(s.Y + s.Height));
            }
        }
        if (schema.Labels != null) 
        {
            foreach (var l in schema.Labels) 
            {
                minX = Math.Min(minX, (float)l.X);
                minY = Math.Min(minY, (float)l.Y);
                maxX = Math.Max(maxX, (float)(l.X + l.Width));
                maxY = Math.Max(maxY, (float)(l.Y + l.Height));
            }
        }

        // padding and scale (leave room for footer)
        float padding = 20f;
        minX -= padding; minY -= padding; maxX += padding; maxY += padding;

        // Multi-page PDF: no scaling, keep objects at actual size
        // Calculate page grid based on object positions
        const float PAGE_GAP = 20f; // Must match JavaScript
        float totalPageWidth = (widthInches * 96f) + PAGE_GAP;
        float totalPageHeight = (heightInches * 96f) + PAGE_GAP;
        
        // Determine how many pages we need
        int maxCol = (int)Math.Ceiling(maxX / totalPageWidth);
        int maxRow = (int)Math.Ceiling(maxY / totalPageHeight);
        
        // Ensure at least one page
        maxCol = Math.Max(1, maxCol);
        maxRow = Math.Max(1, maxRow);

        return Document.Create(container =>
        {
            for (int row = 0; row < maxRow; row++)
            {
                for (int col = 0; col < maxCol; col++)
                {
                    container.Page(page =>
                    {
                        page.Size(new PageSize(widthPoints, heightPoints));
                        page.Margin(0);
                        
                        // Calculate page boundaries
                        float pageMinX = col * totalPageWidth;
                        float pageMinY = row * totalPageHeight;
                        float pageMaxX = pageMinX + (widthInches * 96f);
                        float pageMaxY = pageMinY + (heightInches * 96f);
                        
                        var svg = BuildSvgForPage(schema, pageMinX, pageMinY, pageMaxX, pageMaxY, 
                            widthPoints, heightPoints, documentName, printedDate ?? DateTime.Now, row, col);
                        
                        page.Content().Element(e => e.Svg(svg));
                    });
                }
            }
        }).GeneratePdf();
    }
    
    private static string BuildSvgForPage(DatabaseSchema schema, float pageMinX, float pageMinY, 
        float pageMaxX, float pageMaxY, float pageWidth, float pageHeight, 
        string documentName, DateTime printedDate, int row, int col)
    {
        var sb = new StringBuilder();
        var ci = CultureInfo.InvariantCulture;

        sb.Append($"<svg xmlns='http://www.w3.org/2000/svg' width='{pageWidth.ToString(ci)}' height='{pageHeight.ToString(ci)}' viewBox='0 0 {pageWidth.ToString(ci)} {pageHeight.ToString(ci)}'>");
        sb.Append($"<rect x='0' y='0' width='{pageWidth.ToString(ci)}' height='{pageHeight.ToString(ci)}' fill='#ffffff'/>");

        // Helper to check if object is on this page
        bool IsOnPage(float x, float y, float w, float h)
        {
            return !(x + w < pageMinX || x > pageMaxX || y + h < pageMinY || y > pageMaxY);
        }

        // blueprint palette
        const string lineColor = "#3B82F6";
        const string subtleLine = "#8AB4FF";
        const string tableBorder = "#0f4c8a";
        const string tableHeader = "#084298";     // deep blue header
        const string viewBorder = "#0b5e3a";
        const string viewHeader = "#0a462f";
        const string textColor = "#07203A";       // dark navy
        const string headerTextColor = "#ffffff";

        // relationship lines (dashed) - only render if both endpoints are on this page
        if (schema.Relationships != null)
        {
            foreach (var rel in schema.Relationships)
            {
                try
                {
                    var all = (schema.Tables ?? Enumerable.Empty<dynamic>()).Cast<dynamic>().Concat((schema.Views ?? Enumerable.Empty<dynamic>()).Cast<dynamic>());
                    var from = all.FirstOrDefault(t => string.Equals((string?)t.FullName, (string?)rel.FromTable, StringComparison.Ordinal));
                    var to = all.FirstOrDefault(t => string.Equals((string?)t.FullName, (string?)rel.ToTable, StringComparison.Ordinal));
                    if (from == null || to == null) continue;

                    // Check if either endpoint is on this page
                    float fx = (float)from.X + 100f;
                    float fy = (float)from.Y + 50f;
                    float tx = (float)to.X + 100f;
                    float ty = (float)to.Y + 50f;
                    
                    bool fromOnPage = fx >= pageMinX && fx <= pageMaxX && fy >= pageMinY && fy <= pageMaxY;
                    bool toOnPage = tx >= pageMinX && tx <= pageMaxX && ty >= pageMinY && ty <= pageMaxY;
                    
                    if (fromOnPage || toOnPage)
                    {
                        // Convert to page-relative coordinates
                        float x1 = fx - pageMinX;
                        float y1 = fy - pageMinY;
                        float x2 = tx - pageMinX;
                        float y2 = ty - pageMinY;
                        
                        sb.Append($"<line x1='{x1.ToString(ci)}' y1='{y1.ToString(ci)}' x2='{x2.ToString(ci)}' y2='{y2.ToString(ci)}' stroke='{subtleLine}' stroke-width='1' stroke-dasharray='6,4' stroke-linecap='round'/>");
                    }
                }
                catch { }
            }
        }

        // Render box helper - no scaling, actual size
        void RenderBox(dynamic item, bool isTable)
        {
            if (item == null) return;

            float x = (float)item.X;
            float y = (float)item.Y;
            float width = 200f;

            // header font & row metrics
            var headerFont = 11f;
            float headerPaddingTotal = 6f;
            float headerHeight = headerFont + headerPaddingTotal;

            var colFont = 8f;
            float rowPaddingTotal = 8f;
            float rowHeight = colFont + rowPaddingTotal;

            int colCount = item.Columns?.Count ?? 0;
            float totalHeight = headerHeight + (colCount * rowHeight);

            // Check if this object is on the current page
            if (!IsOnPage(x, y, width, totalHeight)) return;
            
            // Convert to page-relative coordinates
            float pageX = x - pageMinX;
            float pageY = y - pageMinY;

            string border = isTable ? tableBorder : viewBorder;
            string header = isTable ? tableHeader : viewHeader;
            string label = isTable ? "TABLE" : "VIEW";
            var labelFont = 8f;

            // border & header background (wireframe style)
            sb.Append($"<rect x='{pageX.ToString(ci)}' y='{pageY.ToString(ci)}' width='{width.ToString(ci)}' height='{totalHeight.ToString(ci)}' fill='none' stroke='{border}' stroke-width='1.2'/>");
            sb.Append($"<rect x='{pageX.ToString(ci)}' y='{pageY.ToString(ci)}' width='{width.ToString(ci)}' height='{headerHeight.ToString(ci)}' fill='{header}' opacity='0.98'/>");

            // header baseline (center vertically)
            float headerTextY = pageY + (headerHeight / 2f) + (headerFont / 3f) - 1f;

            // full name left (monospace for technical feel)
            sb.Append($"<text x='{(pageX + 6f).ToString(ci)}' y='{headerTextY.ToString(ci)}' font-family='Consolas, Monaco, \"Courier New\", monospace' font-size='{headerFont.ToString(ci)}' fill='{headerTextColor}' font-weight='700'>{EscapeXml((string?)item.FullName)}</text>");
            // small label right (same row)
            sb.Append($"<text x='{(pageX + width - 6f).ToString(ci)}' y='{headerTextY.ToString(ci)}' font-family='Consolas, Monaco, \"Courier New\", monospace' font-size='{labelFont.ToString(ci)}' fill='{headerTextColor}' text-anchor='end'>{label}</text>");

            // columns: baseline adjustment so text sits centered with top/bottom padding 4px each
            float currentY = pageY + headerHeight + (rowHeight / 2f) + (colFont / 3f);

            if (item.Columns != null)
            {
                foreach (var column in item.Columns)
                {
                    // Removed row border lines to avoid clipping and visual clutter.

                    string nameText = column?.Name ?? "";
                    if (isTable && (column?.IsPrimaryKey ?? false)) nameText = "PK " + nameText;
                    if (!(column?.IsNullable ?? true)) nameText += " *";

                    sb.Append($"<text x='{(pageX + 6f).ToString(ci)}' y='{currentY.ToString(ci)}' font-family='Consolas, Monaco, \"Courier New\", monospace' font-size='{colFont.ToString(ci)}' fill='{textColor}'>{EscapeXml(nameText)}</text>");

                    string dataTypeText = column?.DataType ?? "";
                    if (column?.MaxLength is int ml) dataTypeText += $"({ml})";
                    else if (!string.IsNullOrEmpty(column?.DataType) && (column.DataType.ToLowerInvariant().Contains("char") || column.DataType.ToLowerInvariant().Contains("binary")))
                        dataTypeText += "(max)";

                    float dataTypeX = pageX + width - 6f;
                    sb.Append($"<text x='{dataTypeX.ToString(ci)}' y='{currentY.ToString(ci)}' font-family='Consolas, Monaco, \"Courier New\", monospace' font-size='{colFont.ToString(ci)}' fill='{textColor}' text-anchor='end'>{EscapeXml(dataTypeText)}</text>");

                    currentY += rowHeight;
                }
            }
        }

        if (schema.Tables != null) foreach (var t in schema.Tables) RenderBox(t, true);
        if (schema.Views != null) foreach (var v in schema.Views) RenderBox(v, false);

        // Render Shapes
        if (schema.Shapes != null)
        {
            foreach (var shape in schema.Shapes)
            {
                float x = (float)shape.X;
                float y = (float)shape.Y;
                float w = (float)shape.Width;
                float h = (float)shape.Height;
                
                if (!IsOnPage(x, y, w, h)) continue;
                
                float pageX = x - pageMinX;
                float pageY = y - pageMinY;
                
                string fill = shape.FillColor == "transparent" ? "none" : shape.FillColor;
                string stroke = shape.BorderColor;
                float strokeWidth = shape.BorderWidth;

                sb.Append($"<rect x='{pageX.ToString(ci)}' y='{pageY.ToString(ci)}' width='{w.ToString(ci)}' height='{h.ToString(ci)}' fill='{fill}' stroke='{stroke}' stroke-width='{strokeWidth}' rx='4' />");
            }
        }

        // Render Labels
        if (schema.Labels != null)
        {
            foreach (var label in schema.Labels)
            {
                float x = (float)label.X;
                float y = (float)label.Y;
                float w = (float)label.Width;
                float h = (float)label.Height;
                
                if (!IsOnPage(x, y, w, h)) continue;
                
                float pageX = x - pageMinX;
                float pageY = y - pageMinY;
                
                string bg = GetHexColor(label.Color);
                
                // Background
                sb.Append($"<rect x='{pageX.ToString(ci)}' y='{pageY.ToString(ci)}' width='{w.ToString(ci)}' height='{h.ToString(ci)}' fill='{bg}' stroke='#e5e7eb' stroke-width='1' rx='4' />");
                
                // Text - use SVG text instead of foreignObject for better PDF compatibility
                float fontSize = 12f;
                float textX = pageX + 6f;
                float textY = pageY + fontSize + 4f; // Position text with padding
                
                // Split text into lines if needed (simple word wrap)
                var words = label.Text.Split(' ');
                var lines = new List<string>();
                var currentLine = "";
                float maxWidth = w - 12f; // Account for padding
                
                foreach (var word in words)
                {
                    var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                    // Rough estimate: 6 pixels per character
                    if (testLine.Length * 6 > maxWidth && !string.IsNullOrEmpty(currentLine))
                    {
                        lines.Add(currentLine);
                        currentLine = word;
                    }
                    else
                    {
                        currentLine = testLine;
                    }
                }
                if (!string.IsNullOrEmpty(currentLine))
                    lines.Add(currentLine);
                
                // Render each line
                foreach (var line in lines)
                {
                    sb.Append($"<text x='{textX.ToString(ci)}' y='{textY.ToString(ci)}' font-family='sans-serif' font-size='{fontSize.ToString(ci)}' fill='#374151'>{EscapeXml(line)}</text>");
                    textY += fontSize + 2f; // Line height
                    if (textY > pageY + h) break; // Stop if we exceed label height
                }
            }
        }

        // Footer area — architectural-style title block:
        // Left: company, Center: document name, Right: printed date
        var footerFont = 9f;
        var footerY = pageHeight - 12f;
        // company left
        sb.Append($"<text x='12' y='{footerY.ToString(ci)}' font-family='Consolas, Monaco, \"Courier New\", monospace' font-size='{footerFont.ToString(ci)}' fill='{lineColor}' font-weight='700'>Schema Diagram Viewer</text>");
        // document name centered
        sb.Append($"<text x='{(pageWidth / 2f).ToString(ci)}' y='{footerY.ToString(ci)}' font-family='Consolas, Monaco, \"Courier New\", monospace' font-size='{footerFont.ToString(ci)}' fill='#0f4c8a' text-anchor='middle'>{EscapeXml(documentName)}</text>");
        // printed date right
        sb.Append($"<text x='{(pageWidth - 12f).ToString(ci)}' y='{footerY.ToString(ci)}' font-family='Consolas, Monaco, \"Courier New\", monospace' font-size='{footerFont.ToString(ci)}' fill='#6B7280' text-anchor='end'>Printed: {EscapeXml(printedDate.ToString("yyyy-MM-dd HH:mm", ci))}</text>");

        sb.Append("</svg>");
        return sb.ToString();
    }

    private static string EscapeXml(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return System.Security.SecurityElement.Escape(value) ?? string.Empty;
    }

    private static string GetHexColor(string tailwindClass)
    {
        return tailwindClass switch
        {
            "bg-yellow-100" => "#fef9c3",
            "bg-blue-100" => "#dbeafe",
            "bg-green-100" => "#dcfce7",
            "bg-red-100" => "#fee2e2",
            "bg-gray-100" => "#f3f4f6",
            "bg-purple-100" => "#f3e8ff",
            "bg-pink-100" => "#fce7f3",
            _ => "#ffffff"
        };
    }
}
