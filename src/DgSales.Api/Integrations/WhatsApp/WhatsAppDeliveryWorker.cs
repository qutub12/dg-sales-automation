using DgSales.Api.Application;
using DgSales.Api.Domain;
using DgSales.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DgSales.Api.Integrations.WhatsApp;

public sealed class WhatsAppDeliveryWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    TimeProvider timeProvider,
    FollowUpScheduleService followUps,
    ILogger<WhatsAppDeliveryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (configuration.GetValue<bool>("WhatsApp:Enabled"))
            {
                try { await ProcessOneAsync(stoppingToken); }
                catch (Exception exception) { logger.LogError(exception, "WhatsApp delivery worker iteration failed."); }
            }
            await Task.Delay(TimeSpan.FromSeconds(10), timeProvider, stoppingToken);
        }
    }

    private async Task ProcessOneAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
        if (await db.AutomationControls.AnyAsync(x => x.Name == "whatsapp" && x.IsPaused, cancellationToken)) return;
        var provider = scope.ServiceProvider.GetRequiredService<IWhatsAppProvider>();
        var tokens = scope.ServiceProvider.GetRequiredService<QuotationDocumentTokenService>();
        var now = timeProvider.GetUtcNow();
        var job = await db.WhatsAppDeliveryJobs
            .Where(x => x.Status == WhatsAppDeliveryStatus.Queued && x.ScheduledAtUtc <= now)
            .OrderBy(x => x.ScheduledAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (job is null) return;

        job.MarkSending();
        await db.SaveChangesAsync(cancellationToken);
        try
        {
            var quotation = await db.Quotations.SingleAsync(x => x.Id == job.QuotationId, cancellationToken);
            var lead = await db.Leads.SingleAsync(x => x.Id == job.LeadId, cancellationToken);
            if (!lead.ContactAllowed) throw new InvalidOperationException("Contact is blocked for this customer.");
            var baseUrl = configuration["QuotationDocuments:PublicBaseUrl"]?.TrimEnd('/')
                ?? throw new InvalidOperationException("QuotationDocuments:PublicBaseUrl is required.");
            var token = tokens.Create(quotation.Id, TimeSpan.FromHours(24));
            var documentUri = new Uri($"{baseUrl}/api/public/quotations/{quotation.Id}/pdf?expires={token.ExpiresUnix}&signature={token.Signature}");
            var (template, language) = TemplateFor(lead.PreferredLanguage);
            var result = await provider.SendDocumentAsync(new(
                lead.Id, lead.Phone, template, language, documentUri, $"{quotation.QuotationNumber}.pdf"), cancellationToken);
            job.MarkSent(result.ProviderMessageId);
            quotation.MarkSent();
            lead.MarkQuotationSent();
            foreach (var step in followUps.Create(now))
                db.FollowUpJobs.Add(FollowUpJob.Queue(lead.Id, quotation.Id, step.Day, step.Purpose, step.ScheduledAtUtc));
        }
        catch (Exception exception)
        {
            var retry = job.AttemptCount < 3;
            job.MarkFailed(exception.Message, retry, now.AddMinutes(Math.Pow(3, job.AttemptCount)));
            logger.LogWarning(exception, "WhatsApp delivery {DeliveryJobId} failed on attempt {Attempt}.", job.Id, job.AttemptCount);
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private (string Template, string Language) TemplateFor(PreferredLanguage language) => language switch
    {
        PreferredLanguage.Hindi => (Required("WhatsApp:Templates:Hindi:Name"), configuration["WhatsApp:Templates:Hindi:Language"] ?? "hi"),
        PreferredLanguage.Marathi => (Required("WhatsApp:Templates:Marathi:Name"), configuration["WhatsApp:Templates:Marathi:Language"] ?? "mr"),
        _ => (Required("WhatsApp:Templates:English:Name"), configuration["WhatsApp:Templates:English:Language"] ?? "en_US")
    };

    private string Required(string key) => string.IsNullOrWhiteSpace(configuration[key])
        ? throw new InvalidOperationException($"{key} is required when WhatsApp delivery is enabled.")
        : configuration[key]!;
}
