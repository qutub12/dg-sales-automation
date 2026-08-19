namespace DgSales.Api.Application;

public sealed class ServiceAreaMatcher
{
    private static readonly string[] SupportedCities =
    [
        "nagpur", "bhandara", "gondia", "amravati", "akola", "washim", "chandrapur", "yavatmal"
    ];

    public bool IsSupported(string? city)
    {
        if (string.IsNullOrWhiteSpace(city)) return false;
        var normalized = city.Trim().ToLowerInvariant();
        return SupportedCities.Any(x => normalized.Contains(x, StringComparison.Ordinal));
    }
}
