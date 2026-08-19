using DgSales.Api.Domain;
using Xunit;

namespace DgSales.Domain.Tests;

public sealed class WhatsAppDeliveryJobTests
{
    [Fact]
    public void SuccessfulDeliveryStoresProviderMessageId()
    {
        var job = WhatsAppDeliveryJob.Queue(Guid.NewGuid(), Guid.NewGuid());

        job.MarkSending();
        job.MarkSent("wamid.test");

        Assert.Equal(WhatsAppDeliveryStatus.Sent, job.Status);
        Assert.Equal(1, job.AttemptCount);
        Assert.Equal("wamid.test", job.ProviderMessageId);
    }

    [Fact]
    public void RetriableFailureReturnsJobToQueue()
    {
        var job = WhatsAppDeliveryJob.Queue(Guid.NewGuid(), Guid.NewGuid());
        var retryAt = DateTimeOffset.UtcNow.AddMinutes(3);

        job.MarkSending();
        job.MarkFailed("temporary", true, retryAt);

        Assert.Equal(WhatsAppDeliveryStatus.Queued, job.Status);
        Assert.Equal(retryAt, job.ScheduledAtUtc);
    }
}
