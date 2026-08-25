namespace DgSales.Api.Domain;

public sealed record CaptureRequirementRequest(
    decimal? RequestedKva,
    int PhaseCount,
    string? PreferredBrand,
    string Application,
    string InstallationLocation,
    bool SizingConfirmed,
    bool CustomDiscountRequested,
    bool NonStandardTermsRequested,
    bool DeliveryPromiseRequired,
    IReadOnlyCollection<string>? ValidationFlags,
    bool InstallationRequired = false)
{
    public string? Validate()
    {
        if (RequestedKva is <= 0) return "Requested kVA must be greater than zero when supplied.";
        if (PhaseCount is not 1 and not 3) return "Phase count must be 1 or 3.";
        if (string.IsNullOrWhiteSpace(Application)) return "Application is required.";
        if (string.IsNullOrWhiteSpace(InstallationLocation)) return "Installation location is required.";
        return null;
    }
}

public sealed class CustomerRequirement
{
    private CustomerRequirement() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid LeadId { get; private set; }
    public decimal? RequestedKva { get; private set; }
    public int PhaseCount { get; private set; }
    public string? PreferredBrand { get; private set; }
    public string Application { get; private set; } = string.Empty;
    public string InstallationLocation { get; private set; } = string.Empty;
    public bool SizingConfirmed { get; private set; }
    public bool CustomDiscountRequested { get; private set; }
    public bool NonStandardTermsRequested { get; private set; }
    public bool DeliveryPromiseRequired { get; private set; }
    public bool InstallationRequired { get; private set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public string ValidationFlagsJson { get; private set; } = "[]";
    public DateTimeOffset CapturedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    public bool IsComplete => RequestedKva > 0
        && PhaseCount is 1 or 3
        && !string.IsNullOrWhiteSpace(Application)
        && !string.IsNullOrWhiteSpace(InstallationLocation);

    public bool HasValidationFlags => ValidationFlags.Count > 0;
    public IReadOnlyCollection<string> ValidationFlags =>
        System.Text.Json.JsonSerializer.Deserialize<string[]>(ValidationFlagsJson) ?? [];

    public static CustomerRequirement Capture(Guid leadId, CaptureRequirementRequest request) => new()
    {
        LeadId = leadId,
        RequestedKva = request.RequestedKva,
        PhaseCount = request.PhaseCount,
        PreferredBrand = Clean(request.PreferredBrand),
        Application = request.Application.Trim(),
        InstallationLocation = request.InstallationLocation.Trim(),
        SizingConfirmed = request.SizingConfirmed,
        CustomDiscountRequested = request.CustomDiscountRequested,
        NonStandardTermsRequested = request.NonStandardTermsRequested,
        DeliveryPromiseRequired = request.DeliveryPromiseRequired,
        InstallationRequired = request.InstallationRequired,
        ValidationFlagsJson = System.Text.Json.JsonSerializer.Serialize(
            request.ValidationFlags?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().ToArray() ?? [])
    };

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
