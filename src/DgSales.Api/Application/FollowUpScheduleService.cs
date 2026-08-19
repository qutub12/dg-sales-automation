namespace DgSales.Api.Application;

public sealed record FollowUpStep(int Day, string Purpose, DateTimeOffset ScheduledAtUtc);

public sealed class FollowUpScheduleService
{
    public IReadOnlyCollection<FollowUpStep> Create(DateTimeOffset quotationSentAtUtc) =>
    [
        new(1, "Confirm quotation receipt and answer initial questions.", quotationSentAtUtc.AddDays(1)),
        new(3, "Check interest, technical questions and decision timeline.", quotationSentAtUtc.AddDays(3)),
        new(6, "Final active follow-up before disposition.", quotationSentAtUtc.AddDays(6)),
        new(7, "Close, postpone or escalate the lead.", quotationSentAtUtc.AddDays(7))
    ];
}
