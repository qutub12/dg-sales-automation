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
    PreferredLanguage DetectedLanguage);

public sealed record VoiceRequirementToolResult(
    bool Saved, string NextAction, string? QuotationNumber, IReadOnlyCollection<string> ReviewReasons);

public sealed class VoiceRequirementToolService(
    SalesDbContext db,
    ApprovedPriceCatalogueService prices,
    QuotationEligibilityService eligibility,
    StandardQuotationCalculator calculator)
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
            arguments.DeliveryPromiseRequired, arguments.ValidationFlags);
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

        var price = requirement.RequestedKva is { } kva
            ? prices.Find(kva, requirement.PhaseCount, requirement.PreferredBrand)
            : null;
        var assessment = eligibility.Assess(new(
            requirement.IsComplete,
            requirement.SizingConfirmed,
            price is not null,
            price is not null,
            requirement.CustomDiscountRequested,
            requirement.NonStandardTermsRequested,
            requirement.DeliveryPromiseRequired,
            requirement.HasValidationFlags || !arguments.AutomationDisclosed));
        if (!assessment.CanSendAutomatically)
        {
            lead.MarkEscalated();
            await db.SaveChangesAsync(cancellationToken);
            return new(true, "review_required", null, assessment.ReviewReasons);
        }

        var calculated = calculator.Calculate(new(
            price!.BasePrice, price.StandardMarkup, price.TransportCharge,
            price.InstallationCharge, price.AccessoryCharge, price.GstPercent));
        if (!calculated.IsValid)
        {
            lead.MarkEscalated();
            await db.SaveChangesAsync(cancellationToken);
            return new(true, "review_required", null, calculated.Errors);
        }

        var quotation = Quotation.Generate(
            job.LeadId, requirement.Id, price.Version, price.Brand, price.GensetModel,
            price.Kva, price.PhaseCount, calculated.Subtotal, calculated.GstAmount, calculated.GrandTotal);
        db.Quotations.Add(quotation);
        db.WhatsAppDeliveryJobs.Add(WhatsAppDeliveryJob.Queue(quotation.Id, job.LeadId));
        lead.MarkQuotationPending();
        await db.SaveChangesAsync(cancellationToken);
        return new(true, "quotation_queued_for_whatsapp", quotation.QuotationNumber, []);
    }
}
