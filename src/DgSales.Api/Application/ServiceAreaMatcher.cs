namespace DgSales.Api.Application;

public sealed class ServiceAreaMatcher(IConfiguration? configuration = null)
{
    private static readonly string[] DefaultCities =
    [
        "nagpur", "bhandara", "gondia", "amravati", "akola", "washim", "chandrapur", "yavatmal"
    ];

    public bool IsSupported(string? city)
    {
        if (string.IsNullOrWhiteSpace(city)) return false;
        var normalized = city.Trim().ToLowerInvariant();
        var configured = configuration?.GetSection("Business:ServiceAreas").Get<string[]>();
        return (configured is { Length: > 0 } ? configured : DefaultCities)
            .Any(x => normalized.Contains(x.Trim().ToLowerInvariant(), StringComparison.Ordinal));
    }
}
