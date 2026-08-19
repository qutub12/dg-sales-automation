using System.Text.RegularExpressions;

namespace DgSales.Api.Application;

public sealed record ParsedLeadMessage(string? CustomerName, string? Phone, string? City, string? SourceReference, string? Product, IReadOnlyList<string> Flags);

public sealed partial class LeadMessageParser
{
    public ParsedLeadMessage Parse(string text)
    {
        text ??= string.Empty;
        var name = Value(text, "(?:customer|buyer|contact\u0020person|name|regards)");
        var phone = Value(text, "(?:mobile|phone|contact\u0020number|contact)");
        var city = Value(text, "(?:city|location)");
        var reference = Value(text, "(?:lead|enquiry|inquiry)(?:\u0020id|\u0020number|\u0020no)?");
        var product = Value(text, @"(?:product|query|requirement|looking\u0020for|power\s*\(kva\)|brand|probable\s+requirement\s+type)");
        phone = NormalizeCandidate(phone) ?? UnambiguousPhone(text);
        var flags = new List<string>();
        if (string.IsNullOrWhiteSpace(name)) flags.Add("CustomerNameMissing");
        if (phone is null) flags.Add("PhoneMissingOrAmbiguous");
        return new(name, phone, city, reference, product, flags);
    }

    private static string? Value(string text, string label)
    {
        var match = Regex.Match(text, $@"(?im)^\s*{label}\s*[:\-]\s*(?<value>[^\r\n]+)");
        return match.Success ? match.Groups["value"].Value.Trim() : null;
    }

    private static string? NormalizeCandidate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length == 12 && digits.StartsWith("91")) digits = digits[2..];
        return digits.Length == 10 && digits[0] is >= '6' and <= '9' ? digits : null;
    }

    private static string? UnambiguousPhone(string text)
    {
        var candidates = PhoneRegex().Matches(text).Select(x => NormalizeCandidate(x.Value)).Where(x => x is not null).Distinct().ToArray();
        return candidates.Length == 1 ? candidates[0] : null;
    }

    [GeneratedRegex(@"(?<!\d)(?:\+?91[\s-]?)?[6-9]\d{4}[\s-]?\d{5}(?!\d)")]
    private static partial Regex PhoneRegex();
}
