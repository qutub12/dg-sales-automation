namespace DgSales.Api.Domain;

public enum InboundLeadChannel { IndiaMartEmail, JustdialWhatsApp, JustdialPortal }
public enum InboundLeadStatus { Received, LeadCreated, DuplicateLead, ReviewRequired, Failed }

public sealed class InboundLeadMessage
{
    private InboundLeadMessage() { }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public InboundLeadChannel Channel { get; private set; }
    public string ExternalMessageId { get; private set; } = string.Empty;
    public string? Sender { get; private set; }
    public string? Subject { get; private set; }
    public string RawText { get; private set; } = string.Empty;
    public InboundLeadStatus Status { get; private set; } = InboundLeadStatus.Received;
    public Guid? LeadId { get; private set; }
    public string? ProcessingNote { get; private set; }
    public DateTimeOffset ReceivedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAtUtc { get; private set; }

    public static InboundLeadMessage Receive(InboundLeadChannel channel, string externalId, string? sender, string? subject, string rawText) => new()
    {
        Channel = channel, ExternalMessageId = externalId.Trim(), Sender = sender?.Trim(),
        Subject = subject?.Trim(), RawText = rawText.Length > 50000 ? rawText[..50000] : rawText
    };

    public void Complete(InboundLeadStatus status, Guid? leadId, string? note)
    {
        Status = status; LeadId = leadId; ProcessingNote = note; ProcessedAtUtc = DateTimeOffset.UtcNow;
    }
}
