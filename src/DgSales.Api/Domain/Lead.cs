namespace DgSales.Api.Domain;

public enum LeadSource { IndiaMartEmail, IndiaMartApi, JustdialWhatsApp, Manual }
public enum LeadStatus { New, CallQueued, Calling, Qualified, QuotationPending, QuotationSent, FollowUp, Won, Lost, Escalated }
public enum PreferredLanguage { Unknown, Hindi, English, Marathi, Mixed }

public sealed record CreateLeadRequest(
    string CustomerName,
    string Phone,
    string? City,
    LeadSource Source,
    string? SourceReference)
{
    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(CustomerName)) return "Customer name is required.";
        var digits = new string((Phone ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits.Length is < 10 or > 13 ? "A valid Indian phone number is required." : null;
    }
}

public sealed class Lead
{
    private Lead() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string CustomerName { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string? City { get; private set; }
    public LeadSource Source { get; private set; }
    public string? SourceReference { get; private set; }
    public LeadStatus Status { get; private set; } = LeadStatus.New;
    public PreferredLanguage PreferredLanguage { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public uint Version { get; private set; }

    public static Lead Create(CreateLeadRequest request) => new()
    {
        CustomerName = request.CustomerName.Trim(),
        Phone = NormalizePhone(request.Phone),
        City = request.City?.Trim(),
        Source = request.Source,
        SourceReference = request.SourceReference?.Trim()
    };

    public void QueueCall()
    {
        if (Status is not LeadStatus.New and not LeadStatus.FollowUp)
            throw new InvalidOperationException($"A call cannot be queued while lead is {Status}.");
        Status = LeadStatus.CallQueued;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void SetPreferredLanguage(PreferredLanguage language)
    {
        PreferredLanguage = language;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static string NormalizePhone(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length == 10 ? $"+91{digits}" : $"+{digits}";
    }
}
