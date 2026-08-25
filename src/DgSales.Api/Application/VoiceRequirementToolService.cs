using System.Text.Json;
using DgSales.Api.Domain;
using DgSales.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DgSales.Api.Application;

public sealed record VoiceRequirementToolArguments(
    decimal? RequestedKva,
    int PhaseCount,
    string? PreferredBrand,
    string Application,
    string InstallationLocation,
    bool SizingConfirmed,
    bool CustomDiscountRequested,
    bool NonStandardTermsRequested,
    bool DeliveryPromiseRequired,
    string[]? ValidationFlags,
    bool AutomationDisclosed,
    bool RecordingConsentGiven,
    PreferredLanguage DetectedLanguage,
    bool InstallationRequired = false);

public sealed record VoiceRequirementToolResult(
    bool Saved, string NextAction, string? QuotationNumber, IReadOnlyCollection<string> ReviewReasons);

public sealed class VoiceRequirementToolService(
    SalesDbContext db,
    OwnerQuotationWorkflowService ownerWorkflow)
{
    public async Task<VoiceRequirementToolResult> ExecuteAsync(
        string providerCallId, string argumentsJson, CancellationToken cancellationToken)
    {
        var arguments = JsonSerializer.Deserialize<VoiceRequirementToolArguments>(
            argumentsJson, new JsonSerializerOptions(JsonSerializerDefaults.Web) { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } });
        if (arguments is null) return new(false, "review_required", null, ["Structured requirement was empty."]);
        var job = await db.CallJobs.SingleOrDefaultAsync(x => x.ProviderCallId == providerCallId, cancellationToken);
        if (job is null) return new(false, "review_required", null, ["Call job could not be identified."]);
        if (await db.VoiceCallResults.AnyAsync(x => x.CallJobId == job.Id, cancellationToken))
            return new(true, "already_processed", null, []);

        var request = new CaptureRequirementRequest(
            arguments.RequestedKva, arguments.PhaseCount, arguments.PreferredBrand,
            arguments.Application, arguments.InstallationLocation, arguments.SizingConfirmed,
            arguments.CustomDiscountRequested, arguments.NonStandardTermsRequested,
            arguments.DeliveryPromiseRequired, arguments.ValidationFlags, arguments.InstallationRequired);
        if (request.Validate() is { } validationError)
            return new(false, "review_required", null, [validationError]);

        var requirement = CustomerRequirement.Capture(job.LeadId, request);
        var lead = await db.Leads.SingleAsync(x => x.Id == job.LeadId, cancellationToken);
        lead.SetPreferredLanguage(arguments.DetectedLanguage);
        lead.MarkQualified();
        db.CustomerRequirements.Add(requirement);
        var callResult = VoiceCallResult.Capture(
            job.Id, job.LeadId, providerCallId, VoiceCallOutcome.Completed, arguments.DetectedLanguage,
            arguments.AutomationDisclosed, arguments.RecordingConsentGiven, null);
        db.VoiceCallResults.Add(callResult);
        if (job.Status == CallJobStatus.InProgress) job.MarkCompleted();

        if (!arguments.AutomationDisclosed)
        {
            lead.MarkEscalated();
            await db.SaveChangesAsync(cancellationToken);
            return new(true, "review_required", null, ["Automation disclosure was not confirmed."]);
        }
        await db.SaveChangesAsync(cancellationToken);
        var queued = await ownerWorkflow.QueueAsync(lead, requirement, cancellationToken);
        if (queued.Error is not null)
        {
            lead.MarkEscalated();
            await db.SaveChangesAsync(cancellationToken);
            return new(true, "review_required", null, [queued.Error]);
        }
        return new(true, "owner_pricing_requested_on_whatsapp", null, []);
    }
}
