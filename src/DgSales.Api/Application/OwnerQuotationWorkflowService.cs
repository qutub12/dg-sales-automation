using DgSales.Api.Domain;
using DgSales.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DgSales.Api.Application;

public sealed class OwnerQuotationWorkflowService(
    SalesDbContext db,
    ReferenceCatalogueService catalogue,
    StandardQuotationCalculator calculator,
    OwnerQuotationReplyParser parser,
    IConfiguration configuration)
{
    public async Task<(OwnerQuotationApproval? Request, string? Error, bool Duplicate)> QueueAsync(
        Lead lead, CustomerRequirement requirement, CancellationToken cancellationToken)
    {
        var existing = await db.OwnerQuotationApprovals.SingleOrDefaultAsync(
            x => x.RequirementId == requirement.Id, cancellationToken);
        if (existing is not null) return (existing, null, true);
        if (!requirement.IsComplete || !requirement.SizingConfirmed || requirement.HasValidationFlags)
            return (null, "Requirement or generator sizing still requires review.", false);
        if (string.IsNullOrWhiteSpace(requirement.PreferredBrand))
            return (null, "Preferred brand must be confirmed before asking the owner for price.", false);

        var product = catalogue.GetTechnical().Where(x =>
            x.Kva == requirement.RequestedKva && x.PhaseCount == requirement.PhaseCount
            && x.Brand.Equals(requirement.PreferredBrand, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.Compliance.Contains("CPCB IV+", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();
        if (product is null) return (null, "No exact approved technical product matches the requirement.", false);
        if (product.RequiresTechnicalReview) return (null, "The selected product requires technical review before pricing.", false);

        var approval = OwnerQuotationApproval.Queue(lead.Id, requirement.Id, product.Brand,
            product.GensetModel, product.Kva, product.PhaseCount, requirement.InstallationRequired,
            configuration.GetValue("Pricing:GstPercent", 18m));
        db.OwnerQuotationApprovals.Add(approval);
        lead.MarkQuotationPending();
        await db.SaveChangesAsync(cancellationToken);
        return (approval, null, false);
    }

    public async Task<bool> ProcessOwnerReplyAsync(string messageId, string from, string text, CancellationToken cancellationToken)
    {
        if (Digits(from) != Digits(configuration["Business:OwnerEscalationPhone"])) return false;
        var reply = parser.Parse(text);
        if (reply.Kind == OwnerQuotationReplyKind.Unknown || reply.RequestCode is null) return true;
        var approval = await db.OwnerQuotationApprovals.SingleOrDefaultAsync(
            x => x.RequestCode == reply.RequestCode, cancellationToken);
        if (approval is null) return true;

        if (reply.Kind == OwnerQuotationReplyKind.Pricing)
        {
            if (approval.Status != OwnerQuotationApprovalStatus.AwaitingPricing) return true;
            if (approval.InstallationRequired && !text.Contains("INSTALLATION", StringComparison.OrdinalIgnoreCase)) return true;
            approval.SupplyPricing(reply.SellingPrice, reply.TransportCharge, reply.InstallationCharge, text);
            var calculated = calculator.Calculate(new(reply.SellingPrice, 0, reply.TransportCharge,
                reply.InstallationCharge, 0, approval.GstPercent));
            if (!calculated.IsValid) throw new InvalidOperationException(string.Join(" ", calculated.Errors));
            var quotation = Quotation.Generate(approval.LeadId, approval.RequirementId,
                $"OWNER-{approval.RequestCode}", approval.Brand, approval.GensetModel, approval.Kva,
                approval.PhaseCount, reply.SellingPrice, reply.TransportCharge, reply.InstallationCharge,
                calculated.Subtotal, calculated.GstAmount, calculated.GrandTotal);
            quotation.MarkAwaitingOwnerApproval();
            db.Quotations.Add(quotation);
            approval.AttachQuotation(quotation.Id);
        }
        else if (reply.Kind == OwnerQuotationReplyKind.Approve)
        {
            if (approval.Status != OwnerQuotationApprovalStatus.AwaitingApproval) return true;
            if (approval.QuotationId is null) return true;
            approval.Approve(text);
            var quotation = await db.Quotations.SingleAsync(x => x.Id == approval.QuotationId, cancellationToken);
            quotation.MarkApproved();
            if (!await db.WhatsAppDeliveryJobs.AnyAsync(x => x.QuotationId == quotation.Id, cancellationToken))
                db.WhatsAppDeliveryJobs.Add(WhatsAppDeliveryJob.Queue(quotation.Id, quotation.LeadId));
        }
        else if (reply.Kind == OwnerQuotationReplyKind.Reject)
        {
            if (approval.Status != OwnerQuotationApprovalStatus.AwaitingApproval) return true;
            approval.Reject(text);
            if (approval.QuotationId is { } quotationId)
                (await db.Quotations.SingleAsync(x => x.Id == quotationId, cancellationToken)).MarkReviewRequired();
        }
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string Digits(string? value) => new((value ?? string.Empty).Where(char.IsDigit).ToArray());
}
