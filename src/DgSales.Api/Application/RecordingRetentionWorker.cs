using DgSales.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DgSales.Api.Application;

public sealed class RecordingRetentionWorker(
    IServiceScopeFactory scopes,
    IConfiguration configuration,
    TimeProvider clock,
    ILogger<RecordingRetentionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (configuration.GetValue<bool>("Privacy:RecordingRetentionEnabled"))
                try { await PurgeAsync(stoppingToken); }
                catch (Exception ex) { logger.LogError(ex, "Recording transcript retention cleanup failed."); }
            await Task.Delay(TimeSpan.FromHours(24), clock, stoppingToken);
        }
    }

    private async Task PurgeAsync(CancellationToken cancellationToken)
    {
        var days = Math.Clamp(configuration.GetValue("Privacy:RecordingRetentionDays", 30), 1, 365);
        var cutoff = clock.GetUtcNow().AddDays(-days);
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
        var cleared = await db.VoiceCallResults
            .Where(x => x.CompletedAtUtc < cutoff && x.Transcript != null)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.Transcript, (string?)null), cancellationToken);
        if (cleared > 0) logger.LogInformation("Cleared {Count} expired call transcripts.", cleared);
    }
}
