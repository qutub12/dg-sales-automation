using DgSales.Api.Application;
using DgSales.Api.Domain;
using Xunit;

namespace DgSales.Domain.Tests;

public sealed class CustomerReplyServiceTests
{
    [Theory]
    [InlineData("I am interested, call me", CustomerReplyDisposition.HumanHelp)]
    [InlineData("Quotation accepted", CustomerReplyDisposition.Accepted)]
    [InlineData("Not interested", CustomerReplyDisposition.Rejected)]
    [InlineData("किंमत कमी होईल का", CustomerReplyDisposition.Interested)]
    [InlineData("Please explain", CustomerReplyDisposition.Other)]
    public void ClassifiesRepliesWithoutAnLlm(string text, CustomerReplyDisposition expected) =>
        Assert.Equal(expected, CustomerReplyService.Classify(text));

    [Fact]
    public void FollowUpCanBeCancelledBeforeSending()
    {
        var job = FollowUpJob.Queue(Guid.NewGuid(), Guid.NewGuid(), 1, "Check receipt", DateTimeOffset.UtcNow);
        job.Cancel();
        Assert.Equal(FollowUpStatus.Cancelled, job.Status);
    }
}
