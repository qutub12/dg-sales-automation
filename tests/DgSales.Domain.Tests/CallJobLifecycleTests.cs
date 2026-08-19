using DgSales.Api.Domain;
using Xunit;

namespace DgSales.Domain.Tests;

public sealed class CallJobLifecycleTests
{
    [Fact]
    public void StartedCallCanComplete()
    {
        var job = CallJob.Queue(Guid.NewGuid());
        job.MarkStarted("provider-1");
        job.MarkCompleted();

        Assert.Equal(CallJobStatus.Completed, job.Status);
        Assert.Equal(1, job.AttemptCount);
        Assert.Equal("provider-1", job.ProviderCallId);
    }

    [Fact]
    public void FailedStartCanBeRetried()
    {
        var job = CallJob.Queue(Guid.NewGuid());
        var retryAt = DateTimeOffset.UtcNow.AddMinutes(5);
        job.MarkFailed("temporary", true, retryAt);

        Assert.Equal(CallJobStatus.Queued, job.Status);
        Assert.Equal(1, job.AttemptCount);
        Assert.Equal(retryAt, job.ScheduledAtUtc);
    }
}
