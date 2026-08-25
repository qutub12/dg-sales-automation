using DgSales.Api.Application;
using DgSales.Api.Domain;
using DgSales.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DgSales.Api.Integrations.WhatsApp;

public sealed class OwnerQuotationApprovalWorker(
    IServiceScopeFactory scopes,
    IConfiguration configuration,
    TimeProvider clock,
    ILogger<OwnerQuotationApprovalWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (configuration.GetValue<bool>("WhatsApp:Enabled"))
                try { await ProcessOneAsync(stoppingToken); }
                catch (Exception ex) { logger.LogError(ex, "Owner quotation approval worker iteration failed."); }
            await Task.Delay(TimeSpan.FromSeconds(10), clock, stoppingToken);
        }
    }

    private async Task ProcessOneAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
        if (await db.AutomationControls.AnyAsync(x => x.Name == "whatsapp" && x.IsPaused, cancellationToken)) return;
        var now = clock.GetUtcNow();
        var approval = await db.OwnerQuotationApprovals
            .Where(x => (x.Status == OwnerQuotationApprovalStatus.PricingRequestQueued
                      || x.Status == OwnerQuotationApprovalStatus.ApprovalRequestQueued)
                     && x.ScheduledAtUtc <= now)
            .OrderBy(x => x.ScheduledAtUtc).FirstOrDefaultAsync(cancellationToken);
        if (approval is null) return;
        approval.MarkSending();
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var provider = scope.ServiceProvider.GetRequiredService<IWhatsAppProvider>();
            var ownerPhone = Required("Business:OwnerEscalationPhone");
            var lead = await db.Leads.AsNoTracking().SingleAsync(x => x.Id == approval.LeadId, cancellationToken);
            var requirement = await db.CustomerRequirements.AsNoTracking().SingleAsync(x => x.Id == approval.RequirementId, cancellationToken);
            if (approval.Status == OwnerQuotationApprovalStatus.PricingRequestQueued)
            {
                var template = Required("WhatsApp:OwnerApproval:PricingTemplate:Name");
                var language = configuration["WhatsApp:OwnerApproval:PricingTemplate:Language"] ?? "en_US";
                var specification = $"{approval.Brand} {approval.GensetModel}, {approval.Kva:0.##} kVA, {approval.PhaseCount}-phase";
                var replyFormat = approval.InstallationRequired
                    ? $"PRICE {approval.RequestCode} SELLING <amount> TRANSPORT <amount> INSTALLATION <amount>"
                    : $"PRICE {approval.RequestCode} SELLING <amount> TRANSPORT <amount>";
                var result = await provider.SendTemplateAsync(new(ownerPhone, template, language,
                    [approval.RequestCode, lead.CustomerName, specification, requirement.InstallationLocation,
                     replyFormat]), cancellationToken);
                approval.MarkPricingRequestSent(result.ProviderMessageId);
            }
            else
            {
                if (approval.QuotationId is null) throw new InvalidOperationException("Approval request has no quotation.");
                var quotation = await db.Quotations.AsNoTracking().SingleAsync(x => x.Id == approval.QuotationId, cancellationToken);
                var tokens = scope.ServiceProvider.GetRequiredService<QuotationDocumentTokenService>();
                var baseUrl = Required("QuotationDocuments:PublicBaseUrl").TrimEnd('/');
                var token = tokens.Create(quotation.Id, TimeSpan.FromHours(24));
                var document = new Uri($"{baseUrl}/api/public/quotations/{quotation.Id}/pdf?expires={token.ExpiresUnix}&signature={token.Signature}");
                var template = Required("WhatsApp:OwnerApproval:ApprovalTemplate:Name");
                var language = configuration["WhatsApp:OwnerApproval:ApprovalTemplate:Language"] ?? "en_US";
                var summary = $"Subtotal INR {quotation.Subtotal:N2}; GST 18% INR {quotation.GstAmount:N2}; Total INR {quotation.GrandTotal:N2}";
                var result = await provider.SendDocumentAsync(new(lead.Id, ownerPhone, template, language,
                    document, $"{quotation.QuotationNumber}.pdf",
                    [approval.RequestCode, lead.CustomerName, summary,
                     $"APPROVE {approval.RequestCode}", $"REJECT {approval.RequestCode} <reason>"]), cancellationToken);
                approval.MarkApprovalRequestSent(result.ProviderMessageId);
            }
        }
        catch (Exception ex)
        {
            approval.MarkFailed(ex.Message, approval.AttemptCount < 3, now.AddMinutes(Math.Pow(3, approval.AttemptCount)));
            logger.LogWarning(ex, "Owner quotation request {RequestCode} failed on attempt {Attempt}.", approval.RequestCode, approval.AttemptCount);
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private string Required(string key) => string.IsNullOrWhiteSpace(configuration[key])
        ? throw new InvalidOperationException($"{key} is required when WhatsApp delivery is enabled.")
        : configuration[key]!;
}
