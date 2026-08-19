using DgSales.Api.Domain;
using DgSales.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DgSales.Api.Integrations.WhatsApp;

public sealed class FollowUpDeliveryWorker(IServiceScopeFactory scopes, IConfiguration config, TimeProvider clock, ILogger<FollowUpDeliveryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (config.GetValue<bool>("FollowUps:Enabled") && config.GetValue<bool>("WhatsApp:Enabled"))
                try { await ProcessOneAsync(stoppingToken); } catch (Exception ex) { logger.LogError(ex, "Follow-up worker iteration failed."); }
            await Task.Delay(TimeSpan.FromSeconds(10), clock, stoppingToken);
        }
    }

    private async Task ProcessOneAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
        var now = clock.GetUtcNow();
        var job = await db.FollowUpJobs.Where(x => x.Status == FollowUpStatus.Queued && x.ScheduledAtUtc <= now).OrderBy(x => x.ScheduledAtUtc).FirstOrDefaultAsync(ct);
        if (job is null) return;
        var lead = await db.Leads.SingleAsync(x => x.Id == job.LeadId, ct);
        if (lead.Status is LeadStatus.Won or LeadStatus.Lost or LeadStatus.Escalated) { job.Cancel(); await db.SaveChangesAsync(ct); return; }
        job.MarkSending(); await db.SaveChangesAsync(ct);
        try
        {
            var provider = scope.ServiceProvider.GetRequiredService<IWhatsAppProvider>();
            var (template, language) = Template(lead.PreferredLanguage);
            var sent = await provider.SendTemplateAsync(new(lead.Phone, template, language, [lead.CustomerName, job.Purpose]), ct);
            job.MarkSent(sent.ProviderMessageId);
            if (job.Step == 7) lead.MarkLost(); else lead.MarkFollowUp();
        }
        catch (Exception ex) { job.MarkFailed(ex.Message, job.AttemptCount < 3, now.AddMinutes(Math.Pow(3, job.AttemptCount))); logger.LogWarning(ex, "Follow-up {JobId} failed.", job.Id); }
        await db.SaveChangesAsync(ct);
    }

    private (string, string) Template(PreferredLanguage language)
    {
        var lang = language switch { PreferredLanguage.Hindi => "Hindi", PreferredLanguage.Marathi => "Marathi", _ => "English" };
        var name = config[$"FollowUps:Templates:{lang}:Name"];
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException($"FollowUps:Templates:{lang}:Name is required.");
        return (name, config[$"FollowUps:Templates:{lang}:Language"] ?? (lang == "Hindi" ? "hi" : lang == "Marathi" ? "mr" : "en_US"));
    }
}

public sealed class OwnerNotificationWorker(IServiceScopeFactory scopes, IConfiguration config, TimeProvider clock, ILogger<OwnerNotificationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (config.GetValue<bool>("FollowUps:Enabled") && config.GetValue<bool>("WhatsApp:Enabled"))
                try { await ProcessOneAsync(ct); } catch (Exception ex) { logger.LogError(ex, "Owner notification iteration failed."); }
            await Task.Delay(TimeSpan.FromSeconds(10), clock, ct);
        }
    }
    private async Task ProcessOneAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>(); var now = clock.GetUtcNow();
        var job = await db.OwnerNotificationJobs.Where(x => x.Status == OwnerNotificationStatus.Queued && x.ScheduledAtUtc <= now).OrderBy(x => x.ScheduledAtUtc).FirstOrDefaultAsync(ct); if (job is null) return;
        job.MarkSending(); await db.SaveChangesAsync(ct);
        try
        {
            var lead = await db.Leads.AsNoTracking().SingleAsync(x => x.Id == job.LeadId, ct); var reply = await db.CustomerReplies.AsNoTracking().SingleAsync(x => x.Id == job.CustomerReplyId, ct);
            var phone = config["FollowUps:OwnerPhone"] ?? throw new InvalidOperationException("FollowUps:OwnerPhone is required.");
            var template = config["FollowUps:OwnerTemplate:Name"] ?? throw new InvalidOperationException("FollowUps:OwnerTemplate:Name is required.");
            var sent = await scope.ServiceProvider.GetRequiredService<IWhatsAppProvider>().SendTemplateAsync(new(phone, template, config["FollowUps:OwnerTemplate:Language"] ?? "en_US", [lead.CustomerName, lead.Phone, reply.Disposition.ToString()]), ct);
            job.MarkSent(sent.ProviderMessageId);
        }
        catch (Exception ex) { job.MarkFailed(ex.Message, job.AttemptCount < 3, now.AddMinutes(Math.Pow(3, job.AttemptCount))); logger.LogWarning(ex, "Owner notification {JobId} failed.", job.Id); }
        await db.SaveChangesAsync(ct);
    }
}
