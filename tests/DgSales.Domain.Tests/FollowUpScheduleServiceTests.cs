using DgSales.Api.Application;
using Xunit;

namespace DgSales.Domain.Tests;

public sealed class FollowUpScheduleServiceTests
{
    [Fact]
    public void UsesApprovedDayOneThreeSixSevenCadence()
    {
        var sentAt = new DateTimeOffset(2026, 8, 18, 10, 0, 0, TimeSpan.Zero);
        var schedule = new FollowUpScheduleService().Create(sentAt).ToArray();
        Assert.Equal([1, 3, 6, 7], schedule.Select(x => x.Day).ToArray());
        Assert.Equal(sentAt.AddDays(7), schedule[^1].ScheduledAtUtc);
    }
}
