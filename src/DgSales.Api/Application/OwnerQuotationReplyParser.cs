using System.Globalization;
using System.Text.RegularExpressions;

namespace DgSales.Api.Application;

public enum OwnerQuotationReplyKind { Unknown, Pricing, Approve, Reject }
public sealed record OwnerQuotationReply(OwnerQuotationReplyKind Kind, string? RequestCode,
    decimal SellingPrice = 0, decimal TransportCharge = 0, decimal InstallationCharge = 0);

public sealed partial class OwnerQuotationReplyParser
{
    public OwnerQuotationReply Parse(string text)
    {
        var value = Regex.Replace(text.Trim(), @"\s+", " ");
        var decision = DecisionRegex().Match(value);
        if (decision.Success)
            return new(Enum.Parse<OwnerQuotationReplyKind>(decision.Groups[1].Value, true),
                decision.Groups[2].Value.ToUpperInvariant());

        var pricing = PricingRegex().Match(value.Replace(",", ""));
        if (!pricing.Success) return new(OwnerQuotationReplyKind.Unknown, null);
        return new(OwnerQuotationReplyKind.Pricing, pricing.Groups[1].Value.ToUpperInvariant(),
            Decimal(pricing.Groups[2].Value), Decimal(pricing.Groups[3].Value),
            pricing.Groups[4].Success ? Decimal(pricing.Groups[4].Value) : 0m);
    }

    private static decimal Decimal(string value) => decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);

    [GeneratedRegex(@"^(APPROVE|REJECT)\s+(VSS-[A-Z0-9]{6})(?:\s+.*)?$", RegexOptions.IgnoreCase)]
    private static partial Regex DecisionRegex();

    [GeneratedRegex(@"^PRICE\s+(VSS-[A-Z0-9]{6})\s+SELLING\s+(\d+(?:\.\d{1,2})?)\s+TRANSPORT\s+(\d+(?:\.\d{1,2})?)(?:\s+INSTALLATION\s+(\d+(?:\.\d{1,2})?))?$", RegexOptions.IgnoreCase)]
    private static partial Regex PricingRegex();
}
