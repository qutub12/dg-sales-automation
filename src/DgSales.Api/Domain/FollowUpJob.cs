namespace DgSales.Api.Domain;

public enum FollowUpStatus { Queued, Sending, Sent, Cancelled, Failed }

public sealed class FollowUpJob
{
    private FollowUpJob() { }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid LeadId { get; private set; }
    public Guid QuotationId { get; private set; }
    public int Step { get; private set; }
    public string Purpose { get; private set; } = string.Empty;
    public FollowUpStatus Status { get; private set; } = FollowUpStatus.Queued;
    public DateTimeOffset ScheduledAtUtc { get; private set; }
    public int AttemptCount { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public uint Version { get; private set; }

    public static FollowUpJob Queue(Guid leadId, Guid quotationId, int step, string purpose, DateTimeOffset at) => new()
        { LeadId = leadId, QuotationId = quotationId, Step = step, Purpose = purpose, ScheduledAtUtc = at };
    public void MarkSending() { if (Status != FollowUpStatus.Queued) throw new InvalidOperationException(); Status = FollowUpStatus.Sending; AttemptCount++; }
    public void MarkSent(string id) { Status = FollowUpStatus.Sent; ProviderMessageId = id; LastError = null; }
    public void Cancel() { if (Status == FollowUpStatus.Queued) Status = FollowUpStatus.Cancelled; }
    public void MarkFailed(string error, bool retry, DateTimeOffset retryAt) { LastError = error.Length > 1000 ? error[..1000] : error; Status = retry ? FollowUpStatus.Queued : FollowUpStatus.Failed; if (retry) ScheduledAtUtc = retryAt; }
}

public enum CustomerReplyDisposition { Interested, Accepted, Rejected, HumanHelp, Other }

public sealed class CustomerReply
{
    private CustomerReply() { }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid LeadId { get; private set; }
    public string ExternalMessageId { get; private set; } = string.Empty;
    public string Text { get; private set; } = string.Empty;
    public CustomerReplyDisposition Disposition { get; private set; }
    public DateTimeOffset ReceivedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public static CustomerReply Capture(Guid leadId, string externalId, string text, CustomerReplyDisposition disposition) => new()
        { LeadId = leadId, ExternalMessageId = externalId, Text = text.Length > 5000 ? text[..5000] : text, Disposition = disposition };
}

public enum OwnerNotificationStatus { Queued, Sending, Sent, Failed }
public sealed class OwnerNotificationJob
{
    private OwnerNotificationJob() { }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid LeadId { get; private set; }
    public Guid CustomerReplyId { get; private set; }
    public OwnerNotificationStatus Status { get; private set; } = OwnerNotificationStatus.Queued;
    public int AttemptCount { get; private set; }
    public DateTimeOffset ScheduledAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public string? ProviderMessageId { get; private set; }
    public string? LastError { get; private set; }
    public uint Version { get; private set; }
    public static OwnerNotificationJob Queue(Guid leadId, Guid replyId) => new() { LeadId = leadId, CustomerReplyId = replyId };
    public void MarkSending() { Status = OwnerNotificationStatus.Sending; AttemptCount++; }
    public void MarkSent(string id) { Status = OwnerNotificationStatus.Sent; ProviderMessageId = id; LastError = null; }
    public void MarkFailed(string error, bool retry, DateTimeOffset at) { LastError = error.Length > 1000 ? error[..1000] : error; Status = retry ? OwnerNotificationStatus.Queued : OwnerNotificationStatus.Failed; if (retry) ScheduledAtUtc = at; }
}
