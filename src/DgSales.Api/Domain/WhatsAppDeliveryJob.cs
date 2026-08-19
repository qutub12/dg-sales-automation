namespace DgSales.Api.Domain;

public enum WhatsAppDeliveryStatus { Queued, Sending, Sent, Failed }

public sealed class WhatsAppDeliveryJob
{
    private WhatsAppDeliveryJob() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid QuotationId { get; private set; }
    public Guid LeadId { get; private set; }
    public WhatsAppDeliveryStatus Status { get; private set; } = WhatsAppDeliveryStatus.Queued;
    public int AttemptCount { get; private set; }
    public DateTimeOffset ScheduledAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public string? ProviderMessageId { get; private set; }
    public string? LastError { get; private set; }

    public static WhatsAppDeliveryJob Queue(Guid quotationId, Guid leadId) => new()
    {
        QuotationId = quotationId,
        LeadId = leadId
    };
}
