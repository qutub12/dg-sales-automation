namespace DgSales.Api.Domain;

public enum CallJobStatus { Queued, InProgress, Completed, Failed, Cancelled }

public sealed class CallJob
{
    private CallJob() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid LeadId { get; private set; }
    public CallJobStatus Status { get; private set; } = CallJobStatus.Queued;
    public int AttemptCount { get; private set; }
    public DateTimeOffset ScheduledAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public string? ProviderCallId { get; private set; }
    public string? LastError { get; private set; }

    public static CallJob Queue(Guid leadId, DateTimeOffset? scheduledAtUtc = null) => new()
    {
        LeadId = leadId,
        ScheduledAtUtc = scheduledAtUtc ?? DateTimeOffset.UtcNow
    };
}
