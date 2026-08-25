using System.Text.Json;
using DgSales.Api.Domain;
using DgSales.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DgSales.Api.Application;

public sealed record VoiceCallbackToolArguments(
    DateTimeOffset? CallbackAtUtc,
    string RequestedTimeText,
    PreferredLanguage DetectedLanguage,
    bool AutomationDisclosed);

public sealed record VoiceCallbackToolResult(bool Scheduled, DateTimeOffset CallbackAtUtc, string Message);

public sealed class VoiceCallbackToolService(SalesDbContext db, IConfiguration configuration, TimeProvider clock)
{
    public async Task<VoiceCallbackToolResult> ExecuteAsync(string providerCallId, string argumentsJson, CancellationToken cancellationToken)
    {
        var args = JsonSerializer.Deserialize<VoiceCallbackToolArguments>(argumentsJson,
            new JsonSerializerOptions(JsonSerializerDefaults.Web) { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } })
            ?? throw new ArgumentException("Callback details were empty.");
        var job = await db.CallJobs.SingleOrDefaultAsync(x => x.ProviderCallId == providerCallId, cancellationToken)
            ?? throw new InvalidOperationException("Call job could not be identified.");
        if (await db.VoiceCallResults.AnyAsync(x => x.CallJobId == job.Id, cancellationToken))
            return new(true, clock.GetUtcNow(), "Callback was already processed.");

        var now = clock.GetUtcNow();
        var requested = args.CallbackAtUtc ?? now.AddMinutes(Math.Max(30, configuration.GetValue("Voice:CallbackDefaultMinutes", 120)));
        if (requested < now.AddMinutes(5)) requested = now.AddMinutes(5);
        if (requested > now.AddDays(30)) throw new ArgumentException("Callback time cannot be more than 30 days away.");

        db.VoiceCallResults.Add(VoiceCallResult.Capture(job.Id, job.LeadId, providerCallId,
            VoiceCallOutcome.CustomerRequestedCallback, args.DetectedLanguage, args.AutomationDisclosed,
            false, null, requested));
        if (job.Status == CallJobStatus.InProgress) job.MarkCompleted();
        db.CallJobs.Add(CallJob.Queue(job.LeadId, requested));
        var lead = await db.Leads.SingleAsync(x => x.Id == job.LeadId, cancellationToken);
        lead.SetPreferredLanguage(args.DetectedLanguage);
        await db.SaveChangesAsync(cancellationToken);
        return new(true, requested, "Callback scheduled. End the call politely without continuing sales questions.");
    }
}
