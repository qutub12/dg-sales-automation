using DgSales.Api.Domain;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace DgSales.Api.Application;

public sealed record QuotationBranding(
    string CompanyName,
    string Address,
    string Phone,
    string Email,
    string GstNumber,
    string Terms,
    string FontPath,
    string? BoldFontPath,
    string? LogoPath);

public sealed class QuotationPdfService(IConfiguration configuration)
{
    private static readonly object FontLock = new();
    private static string? _configuredFont;

    public byte[] Render(Quotation quotation, Lead lead, CustomerRequirement requirement)
    {
        var branding = LoadBranding();
        EnsureFontResolver(branding.FontPath, branding.BoldFontPath);

        using var document = new PdfDocument();
        document.Info.Title = $"Quotation {quotation.QuotationNumber}";
        document.Info.Author = branding.CompanyName;
        var page = document.AddPage();
        page.Size = PdfSharp.PageSize.A4;
        using var graphics = XGraphics.FromPdfPage(page);
        var regular = new XFont("DgSalesSans", 10, XFontStyleEx.Regular);
        var small = new XFont("DgSalesSans", 8, XFontStyleEx.Regular);
        var heading = new XFont("DgSalesSans", 18, XFontStyleEx.Bold);
        var subheading = new XFont("DgSalesSans", 11, XFontStyleEx.Bold);
        var dark = XColor.FromArgb(31, 41, 55);
        var accent = XColor.FromArgb(16, 92, 77);

        graphics.DrawRectangle(new XSolidBrush(accent), 0, 0, page.Width.Point, 92);
        var titleX = 42d;
        if (!string.IsNullOrWhiteSpace(branding.LogoPath) && File.Exists(branding.LogoPath))
        {
            using var logo = XImage.FromFile(branding.LogoPath);
            graphics.DrawImage(logo, 42, 18, 105, 53);
            titleX = 166;
        }
        graphics.DrawString(branding.CompanyName, heading, XBrushes.White, new XRect(titleX, 25, 387, 28), XStringFormats.TopLeft);
        graphics.DrawString("DIESEL GENERATOR QUOTATION", subheading, XBrushes.White, new XRect(titleX, 57, 387, 20), XStringFormats.TopLeft);

        var y = 118d;
        DrawPair(graphics, regular, dark, "Quotation No.", quotation.QuotationNumber, 42, y);
        DrawPair(graphics, regular, dark, "Date", quotation.CreatedAtUtc.ToString("dd MMM yyyy"), 330, y);
        y += 28;
        graphics.DrawLine(new XPen(XColors.LightGray), 42, y, 553, y);
        y += 18;

        graphics.DrawString("Customer", subheading, new XSolidBrush(accent), 42, y);
        y += 20;
        graphics.DrawString(Safe(lead.CustomerName), regular, new XSolidBrush(dark), 42, y);
        graphics.DrawString(Safe(lead.Phone), regular, new XSolidBrush(dark), 330, y);
        y += 17;
        graphics.DrawString(Safe(requirement.InstallationLocation), regular, new XSolidBrush(dark), 42, y);
        y += 32;

        DrawTableHeader(graphics, subheading, accent, y);
        y += 28;
        DrawCell(graphics, regular, dark, quotation.Brand, 42, y, 115);
        DrawCell(graphics, regular, dark, quotation.GensetModel, 157, y, 145);
        DrawCell(graphics, regular, dark, $"{quotation.Kva:0.##} kVA", 302, y, 80);
        DrawCell(graphics, regular, dark, $"{quotation.PhaseCount} phase", 382, y, 75);
        DrawCell(graphics, regular, dark, Money(quotation.Subtotal), 457, y, 96, XStringFormats.TopRight);
        y += 40;

        DrawAmount(graphics, regular, dark, "Subtotal", quotation.Subtotal, y); y += 20;
        DrawAmount(graphics, regular, dark, "GST", quotation.GstAmount, y); y += 24;
        graphics.DrawLine(new XPen(accent, 1.5), 350, y, 553, y); y += 10;
        DrawAmount(graphics, subheading, accent, "Grand Total", quotation.GrandTotal, y); y += 42;

        graphics.DrawString("Requirement", subheading, new XSolidBrush(accent), 42, y); y += 20;
        graphics.DrawString(Safe(requirement.Application), regular, new XSolidBrush(dark), 42, y); y += 32;
        graphics.DrawString("Terms", subheading, new XSolidBrush(accent), 42, y); y += 18;
        DrawWrapped(graphics, small, dark, Safe(branding.Terms), 42, ref y, 511);

        var footer = $"{branding.Address} | {branding.Phone} | {branding.Email} | GST: {branding.GstNumber}";
        graphics.DrawLine(new XPen(XColors.LightGray), 42, 790, 553, 790);
        graphics.DrawString(Safe(footer), small, XBrushes.Gray, new XRect(42, 799, 511, 20), XStringFormats.TopCenter);

        using var stream = new MemoryStream();
        document.Save(stream, false);
        return stream.ToArray();
    }

    private QuotationBranding LoadBranding()
    {
        var section = configuration.GetSection("QuotationBranding");
        var fontPath = section["FontPath"];
        if (string.IsNullOrWhiteSpace(fontPath) || !File.Exists(fontPath))
            throw new InvalidOperationException("QuotationBranding:FontPath must point to an installed TrueType font.");
        return new(
            section["CompanyName"] ?? "DG Sales",
            section["Address"] ?? string.Empty,
            section["Phone"] ?? string.Empty,
            section["Email"] ?? string.Empty,
            section["GstNumber"] ?? string.Empty,
            section["Terms"] ?? "Price and delivery are subject to the approved quotation terms.",
            fontPath,
            section["BoldFontPath"],
            section["LogoPath"]);
    }

    private static void EnsureFontResolver(string regularPath, string? boldPath)
    {
        lock (FontLock)
        {
            if (_configuredFont is not null && !string.Equals(_configuredFont, regularPath, StringComparison.Ordinal))
                throw new InvalidOperationException("PDF font cannot be changed after the renderer starts.");
            if (_configuredFont is not null) return;
            GlobalFontSettings.FontResolver = new ConfiguredFontResolver(regularPath, boldPath);
            _configuredFont = regularPath;
        }
    }

    private static void DrawPair(XGraphics g, XFont font, XColor color, string label, string value, double x, double y)
    {
        g.DrawString(label, font, XBrushes.Gray, x, y);
        g.DrawString(Safe(value), font, new XSolidBrush(color), x + 88, y);
    }

    private static void DrawTableHeader(XGraphics g, XFont font, XColor color, double y)
    {
        g.DrawRectangle(new XSolidBrush(color), 42, y, 511, 24);
        foreach (var item in new[] { ("Brand", 48d), ("Model", 163d), ("Capacity", 308d), ("Phase", 388d), ("Amount", 463d) })
            g.DrawString(item.Item1, font, XBrushes.White, item.Item2, y + 16);
    }

    private static void DrawCell(XGraphics g, XFont font, XColor color, string value, double x, double y, double width, XStringFormat? format = null) =>
        g.DrawString(Safe(value), font, new XSolidBrush(color), new XRect(x, y, width, 30), format ?? XStringFormats.TopLeft);

    private static void DrawAmount(XGraphics g, XFont font, XColor color, string label, decimal amount, double y)
    {
        g.DrawString(label, font, new XSolidBrush(color), new XRect(350, y, 95, 18), XStringFormats.TopRight);
        g.DrawString(Money(amount), font, new XSolidBrush(color), new XRect(457, y, 96, 18), XStringFormats.TopRight);
    }

    private static void DrawWrapped(XGraphics g, XFont font, XColor color, string value, double x, ref double y, double maxWidth)
    {
        var line = string.Empty;
        foreach (var word in value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = line.Length == 0 ? word : $"{line} {word}";
            if (g.MeasureString(candidate, font).Width <= maxWidth) { line = candidate; continue; }
            g.DrawString(line, font, new XSolidBrush(color), x, y); y += 14; line = word;
        }
        if (line.Length > 0) { g.DrawString(line, font, new XSolidBrush(color), x, y); y += 14; }
    }

    private static string Money(decimal value) => $"INR {value:N2}";
    private static string Safe(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();

    private sealed class ConfiguredFontResolver(string regularPath, string? boldPath) : IFontResolver
    {
        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic) =>
            new(isBold && !string.IsNullOrWhiteSpace(boldPath) ? "DgSalesBold" : "DgSalesRegular");

        public byte[]? GetFont(string faceName) => faceName == "DgSalesBold" && !string.IsNullOrWhiteSpace(boldPath)
            ? File.ReadAllBytes(boldPath)
            : File.ReadAllBytes(regularPath);
    }
}
