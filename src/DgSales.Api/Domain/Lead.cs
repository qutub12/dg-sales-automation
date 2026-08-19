namespace DgSales.Api.Domain;

public enum LeadSource { IndiaMartEmail, IndiaMartApi, JustdialWhatsApp, Manual }
public enum LeadStatus { New, CallQueued, Calling, Qualified, QuotationPending, QuotationSent, FollowUp, Won, Lost, Escalated }
public enum PreferredLanguage { Unknown, Hindi, English, Marathi, Mixed }
public sealed record UpdateLeadRequest(string CustomerName, string Phone, string? City);

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
    public bool IsInServiceArea { get; private set; }
    public LeadStatus Status { get; private set; } = LeadStatus.New;
    public PreferredLanguage PreferredLanguage { get; private set; }
    public bool ContactAllowed { get; private set; } = true;
    public string? ContactRestrictionReason { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public uint Version { get; private set; }

    public static Lead Create(CreateLeadRequest request, bool isInServiceArea = false) => new()
    {
        CustomerName = request.CustomerName.Trim(),
        Phone = NormalizePhone(request.Phone),
        City = request.City?.Trim(),
        Source = request.Source,
        SourceReference = request.SourceReference?.Trim(),
        IsInServiceArea = isInServiceArea
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

    public void MarkQualified()
    {
        Status = LeadStatus.Qualified;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkQuotationPending()
    {
        Status = LeadStatus.QuotationPending;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkEscalated()
    {
        Status = LeadStatus.Escalated;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkQuotationSent() { Status = LeadStatus.QuotationSent; UpdatedAtUtc = DateTimeOffset.UtcNow; }
    public void MarkFollowUp() { Status = LeadStatus.FollowUp; UpdatedAtUtc = DateTimeOffset.UtcNow; }
    public void MarkWon() { Status = LeadStatus.Won; UpdatedAtUtc = DateTimeOffset.UtcNow; }
    public void MarkLost() { Status = LeadStatus.Lost; UpdatedAtUtc = DateTimeOffset.UtcNow; }
    public void UpdateContact(UpdateLeadRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName)) throw new ArgumentException("Customer name is required.");
        var normalized = NormalizePhone(request.Phone); if (normalized.Length != 13) throw new ArgumentException("Valid Indian mobile number is required.");
        CustomerName = request.CustomerName.Trim(); Phone = normalized; City = request.City?.Trim(); UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
    public void RestrictContact(string reason) { ContactAllowed = false; ContactRestrictionReason = string.IsNullOrWhiteSpace(reason) ? "Customer opted out." : reason.Trim(); UpdatedAtUtc = DateTimeOffset.UtcNow; }
    public void AllowContact() { ContactAllowed = true; ContactRestrictionReason = null; UpdatedAtUtc = DateTimeOffset.UtcNow; }

    public static string NormalizePhone(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length == 10 ? $"+91{digits}" : $"+{digits}";
    }
}
