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
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string CustomerName { get; init; }
    public required string Phone { get; init; }
    public string? City { get; init; }
    public LeadSource Source { get; init; }
    public string? SourceReference { get; init; }
    public LeadStatus Status { get; private set; } = LeadStatus.New;
    public PreferredLanguage PreferredLanguage { get; set; }
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

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
    }

    public static string NormalizePhone(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length == 10 ? $"+91{digits}" : $"+{digits}";
    }
}

