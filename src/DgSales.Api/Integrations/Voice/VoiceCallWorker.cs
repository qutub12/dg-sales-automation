using DgSales.Api.Application;
using DgSales.Api.Domain;
using DgSales.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DgSales.Api.Integrations.Voice;

public sealed class VoiceCallWorker(
    IServiceScopeFactory scopeFactory, IConfiguration configuration, TimeProvider timeProvider,
    VoiceAgentInstructions instructions, ILogger<VoiceCallWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (configuration.GetValue<bool>("Voice:Enabled"))
            {
                try { await ProcessOneAsync(stoppingToken); }
                catch (Exception exception) { logger.LogError(exception, "Voice call worker iteration failed."); }
            }
            await Task.Delay(TimeSpan.FromSeconds(10), timeProvider, stoppingToken);
        }
    }

    private async Task ProcessOneAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
        if (await db.AutomationControls.AnyAsync(x => x.Name == "calls" && x.IsPaused, cancellationToken)) return;
        var provider = scope.ServiceProvider.GetRequiredService<IVoiceCallProvider>();
        var now = timeProvider.GetUtcNow();
        if (!WithinCallingHours(now)) return;
        var job = await db.CallJobs.Where(x => x.Status == CallJobStatus.Queued && x.ScheduledAtUtc <= now)
            .OrderBy(x => x.ScheduledAtUtc).FirstOrDefaultAsync(cancellationToken);
        if (job is null) return;
        var lead = await db.Leads.AsNoTracking().SingleAsync(x => x.Id == job.LeadId, cancellationToken);
        if (!lead.ContactAllowed) { job.Cancel("Contact is blocked for this customer."); await db.SaveChangesAsync(cancellationToken); return; }

        try
        {
            var baseUrl = configuration["Voice:PublicBaseUrl"]?.TrimEnd('/')
                ?? throw new InvalidOperationException("Voice:PublicBaseUrl is required.");
            var webhook = BuildStatusWebhook(baseUrl);
            var language = lead.PreferredLanguage switch
            {
                PreferredLanguage.Hindi => "Hindi",
                PreferredLanguage.Marathi => "Marathi",
                PreferredLanguage.English => "English",
                _ => "Hindi"
            };
            var result = await provider.StartAsync(new(
                job.Id, lead.Id, lead.Phone, lead.CustomerName, lead.City,
                language, webhook, instructions.Build(language)), cancellationToken);
            job.MarkStarted(result.ProviderCallId);
        }
        catch (Exception exception)
        {
            var maximumAttempts = Math.Max(1, configuration.GetValue("Business:Calling:MaximumAttempts", 3));
            var retryMinutes = Math.Max(5, configuration.GetValue("Business:Calling:RetryMinutes", 15));
            job.MarkFailed(exception.Message, job.AttemptCount < maximumAttempts, now.AddMinutes(retryMinutes));
            logger.LogWarning(exception, "Starting voice call {CallJobId} failed.", job.Id);
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private bool WithinCallingHours(DateTimeOffset utcNow)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(configuration["Business:Calling:TimeZone"] ?? "Asia/Kolkata");
        var local = TimeZoneInfo.ConvertTime(utcNow, zone);
        var days = configuration.GetSection("Business:Calling:WorkingDays").Get<string[]>() ?? [];
        if (!days.Contains(local.DayOfWeek.ToString(), StringComparer.OrdinalIgnoreCase)) return false;
        return TimeOnly.TryParse(configuration["Business:Calling:Start"], out var start)
            && TimeOnly.TryParse(configuration["Business:Calling:End"], out var end)
            && TimeOnly.FromDateTime(local.DateTime) >= start && TimeOnly.FromDateTime(local.DateTime) <= end;
    }

    private Uri BuildStatusWebhook(string baseUrl)
    {
        if (!string.Equals(configuration["Voice:Provider"], "Exotel", StringComparison.OrdinalIgnoreCase))
            return new Uri($"{baseUrl}/api/webhooks/voice/call-result");
        var token = configuration["Voice:Exotel:CallbackToken"];
        if (string.IsNullOrWhiteSpace(token) || token.Length < 32)
            throw new InvalidOperationException("Voice:Exotel:CallbackToken must contain at least 32 characters.");
        return new Uri($"{baseUrl}/api/webhooks/voice/exotel-status?token={Uri.EscapeDataString(token)}");
    }
}
