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
    public uint Version { get; private set; }

    public static WhatsAppDeliveryJob Queue(Guid quotationId, Guid leadId) => new()
    {
        QuotationId = quotationId,
        LeadId = leadId
    };

    public void MarkSending()
    {
        if (Status != WhatsAppDeliveryStatus.Queued) throw new InvalidOperationException("Only queued jobs can be sent.");
        Status = WhatsAppDeliveryStatus.Sending;
        AttemptCount++;
    }

    public void MarkSent(string providerMessageId)
    {
        Status = WhatsAppDeliveryStatus.Sent;
        ProviderMessageId = providerMessageId;
        LastError = null;
    }

    public void MarkFailed(string error, bool retry, DateTimeOffset retryAtUtc)
    {
        LastError = error.Length > 1000 ? error[..1000] : error;
        Status = retry ? WhatsAppDeliveryStatus.Queued : WhatsAppDeliveryStatus.Failed;
        if (retry) ScheduledAtUtc = retryAtUtc;
    }
}
