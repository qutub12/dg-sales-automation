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
    public uint Version { get; private set; }

    public static CallJob Queue(Guid leadId, DateTimeOffset? scheduledAtUtc = null) => new()
    {
        LeadId = leadId,
        ScheduledAtUtc = scheduledAtUtc ?? DateTimeOffset.UtcNow
    };

    public void MarkStarted(string providerCallId)
    {
        if (Status != CallJobStatus.Queued) throw new InvalidOperationException("Only queued calls can be started.");
        Status = CallJobStatus.InProgress;
        ProviderCallId = providerCallId;
        AttemptCount++;
        LastError = null;
    }

    public void MarkCompleted()
    {
        if (Status != CallJobStatus.InProgress) throw new InvalidOperationException("Only active calls can complete.");
        Status = CallJobStatus.Completed;
    }

    public void MarkFailed(string error, bool retry, DateTimeOffset retryAtUtc)
    {
        if (Status == CallJobStatus.Queued) AttemptCount++;
        LastError = error.Length > 1000 ? error[..1000] : error;
        Status = retry ? CallJobStatus.Queued : CallJobStatus.Failed;
        if (retry) ScheduledAtUtc = retryAtUtc;
    }
}
