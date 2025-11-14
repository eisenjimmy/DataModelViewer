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

        // padding and scale (leave room for footer)
        float padding = 20f;
        minX -= padding; minY -= padding; maxX += padding; maxY += padding;

        float contentWidth = Math.Max(1f, maxX - minX);
        float contentHeight = Math.Max(1f, maxY - minY);
        float scaleX = (widthPoints - 40f) / contentWidth;
        float scaleY = (heightPoints - 60f) / contentHeight; // extra for footer
        float scale = Math.Min(scaleX, scaleY);

        var svg = BuildSvg(schema, minX, minY, scale, widthPoints, heightPoints, documentName, printedDate ?? DateTime.Now);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(new PageSize(widthPoints, heightPoints));
                page.Margin(0);
                page.Content().Element(e => e.Svg(svg));
            });
        }).GeneratePdf();
    }

    private static string BuildSvg(DatabaseSchema schema, float minX, float minY, float scale, float pageWidth, float pageHeight, string documentName, DateTime printedDate)
    {
        var sb = new StringBuilder();
        var ci = CultureInfo.InvariantCulture;

        sb.Append($"<svg xmlns='http://www.w3.org/2000/svg' width='{pageWidth.ToString(ci)}' height='{pageHeight.ToString(ci)}' viewBox='0 0 {pageWidth.ToString(ci)} {pageHeight.ToString(ci)}'>");
        // white background (no blueprint grid)
        sb.Append($"<rect x='0' y='0' width='{pageWidth.ToString(ci)}' height='{pageHeight.ToString(ci)}' fill='#ffffff'/>");

        // blueprint palette (kept colors but on white background)
        const string lineColor = "#3B82F6";        // blue links
        const string subtleLine = "#8AB4FF";      // lighter dashed
        const string tableBorder = "#0f4c8a";     // navy border
        const string tableHeader = "#084298";     // deep blue header
        const string viewBorder = "#0b5e3a";
        const string viewHeader = "#0a462f";
        const string textColor = "#07203A";       // dark navy
        const string headerTextColor = "#ffffff";

        // relationship lines (dashed)
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

                    float fx = ((float)(from.X - minX) + 100f) * scale;
                    float fy = ((float)(from.Y - minY) + 50f) * scale;
                    float tx = ((float)(to.X - minX) + 100f) * scale;
                    float ty = ((float)(to.Y - minY) + 50f) * scale;

                    sb.Append($"<line x1='{fx.ToString(ci)}' y1='{fy.ToString(ci)}' x2='{tx.ToString(ci)}' y2='{ty.ToString(ci)}' stroke='{subtleLine}' stroke-width='1' stroke-dasharray='6,4' stroke-linecap='round'/>");
                }
                catch { }
            }
        }

        // Render box helper with compact single-row header and balanced row padding (4px top + 4px bottom)
        void RenderBox(dynamic item, bool isTable)
        {
            if (item == null) return;

            float x = ((float)(item.X - minX)) * scale;
            float y = ((float)(item.Y - minY)) * scale;
            float width = 200f * scale;

            // header font & row metrics (more compact)
            var headerFont = Math.Max(9f, 11f * scale);           // slightly smaller header for technical feel
            float headerPaddingTotal = 6f;                        // reduced top gap (3px top + 3px bottom)
            float headerHeight = headerFont + headerPaddingTotal; // single-row header

            var colFont = Math.Max(7f, 8f * scale);
            float rowPaddingTotal = 8f;                           // 4px top + 4px bottom
            float rowHeight = colFont + rowPaddingTotal;

            int colCount = item.Columns?.Count ?? 0;
            float totalHeight = headerHeight + (colCount * rowHeight);

            // clip check
            if (x + width < 0 || y + totalHeight < 0 || x > pageWidth || y > pageHeight) return;

            string border = isTable ? tableBorder : viewBorder;
            string header = isTable ? tableHeader : viewHeader;
            string label = isTable ? "TABLE" : "VIEW";
            var labelFont = Math.Max(7f, 8f * scale);

            // border & header background (wireframe style)
            sb.Append($"<rect x='{x.ToString(ci)}' y='{y.ToString(ci)}' width='{width.ToString(ci)}' height='{totalHeight.ToString(ci)}' fill='none' stroke='{border}' stroke-width='1.2'/>");
            sb.Append($"<rect x='{x.ToString(ci)}' y='{y.ToString(ci)}' width='{width.ToString(ci)}' height='{headerHeight.ToString(ci)}' fill='{header}' opacity='0.98'/>");

            // header baseline (center vertically)
            float headerTextY = y + (headerHeight / 2f) + (headerFont / 3f) - 1f;

            // full name left (monospace for technical feel)
            sb.Append($"<text x='{(x + 6f).ToString(ci)}' y='{headerTextY.ToString(ci)}' font-family='Consolas, Monaco, \"Courier New\", monospace' font-size='{headerFont.ToString(ci)}' fill='{headerTextColor}' font-weight='700'>{EscapeXml((string?)item.FullName)}</text>");
            // small label right (same row)
            sb.Append($"<text x='{(x + width - 6f).ToString(ci)}' y='{headerTextY.ToString(ci)}' font-family='Consolas, Monaco, \"Courier New\", monospace' font-size='{labelFont.ToString(ci)}' fill='{headerTextColor}' text-anchor='end'>{label}</text>");

            // columns: baseline adjustment so text sits centered with top/bottom padding 4px each
            float currentY = y + headerHeight + (rowHeight / 2f) + (colFont / 3f);

            if (item.Columns != null)
            {
                foreach (var column in item.Columns)
                {
                    // Removed row border lines to avoid clipping and visual clutter.

                    string nameText = column?.Name ?? "";
                    if (isTable && (column?.IsPrimaryKey ?? false)) nameText = "PK " + nameText;
                    if (!(column?.IsNullable ?? true)) nameText += " *";

                    sb.Append($"<text x='{(x + 6f).ToString(ci)}' y='{currentY.ToString(ci)}' font-family='Consolas, Monaco, \"Courier New\", monospace' font-size='{colFont.ToString(ci)}' fill='{textColor}'>{EscapeXml(nameText)}</text>");

                    string dataTypeText = column?.DataType ?? "";
                    if (column?.MaxLength is int ml) dataTypeText += $"({ml})";
                    else if (!string.IsNullOrEmpty(column?.DataType) && (column.DataType.ToLowerInvariant().Contains("char") || column.DataType.ToLowerInvariant().Contains("binary")))
                        dataTypeText += "(max)";

                    float dataTypeX = x + width - 6f;
                    sb.Append($"<text x='{dataTypeX.ToString(ci)}' y='{currentY.ToString(ci)}' font-family='Consolas, Monaco, \"Courier New\", monospace' font-size='{colFont.ToString(ci)}' fill='{textColor}' text-anchor='end'>{EscapeXml(dataTypeText)}</text>");

                    currentY += rowHeight;
                }
            }
        }

        if (schema.Tables != null) foreach (var t in schema.Tables) RenderBox(t, true);
        if (schema.Views != null) foreach (var v in schema.Views) RenderBox(v, false);

        // Footer area — architectural-style title block:
        // Left: company, Center: document name, Right: printed date
        var footerFont = 9f;
        var footerY = pageHeight - 12f;
        // company left
        sb.Append($"<text x='12' y='{footerY.ToString(ci)}' font-family='Consolas, Monaco, \"Courier New\", monospace' font-size='{footerFont.ToString(ci)}' fill='{lineColor}' font-weight='700'>Centerview Partners</text>");
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
}
