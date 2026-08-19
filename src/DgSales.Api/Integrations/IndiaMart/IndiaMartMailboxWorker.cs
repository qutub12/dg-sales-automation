using DgSales.Api.Application;
using DgSales.Api.Domain;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;

namespace DgSales.Api.Integrations.IndiaMart;

public sealed class IndiaMartMailboxWorker(IServiceScopeFactory scopes, IConfiguration config, ILogger<IndiaMartMailboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var seconds = Math.Max(30, config.GetValue("LeadIntake:IndiaMart:PollSeconds", 60));
            if (config.GetValue<bool>("LeadIntake:IndiaMart:Enabled"))
                try { await PollAsync(stoppingToken); } catch (Exception ex) { logger.LogError(ex, "IndiaMART mailbox poll failed."); }
            await Task.Delay(TimeSpan.FromSeconds(seconds), stoppingToken);
        }
    }

    private async Task PollAsync(CancellationToken ct)
    {
        var host = Required("Host"); var user = Required("Username"); var password = Required("Password");
        var senderFilter = Required("SenderContains"); var subjectFilter = Required("SubjectContains");
        using var client = new ImapClient();
        await client.ConnectAsync(host, config.GetValue("LeadIntake:IndiaMart:Port", 993), config.GetValue("LeadIntake:IndiaMart:UseSsl", true), ct);
        await client.AuthenticateAsync(user, password, ct);
        var folder = await client.GetFolderAsync(config["LeadIntake:IndiaMart:Folder"] ?? "INBOX", ct);
        await folder.OpenAsync(FolderAccess.ReadWrite, ct);
        foreach (var uid in await folder.SearchAsync(SearchQuery.NotSeen, ct))
        {
            var message = await folder.GetMessageAsync(uid, ct);
            if (!message.From.ToString().Contains(senderFilter, StringComparison.OrdinalIgnoreCase)
                || !(message.Subject ?? "").Contains(subjectFilter, StringComparison.OrdinalIgnoreCase)) continue;
            var body = message.TextBody ?? StripHtml(message.HtmlBody ?? "");
            var externalId = string.IsNullOrWhiteSpace(message.MessageId) ? $"imap:{folder.FullName}:{uid.Id}" : message.MessageId;
            await using var scope = scopes.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<InboundLeadProcessor>().ProcessAsync(
                InboundLeadChannel.IndiaMartEmail, externalId, message.From.ToString(), message.Subject, body, ct);
            await folder.AddFlagsAsync(uid, MessageFlags.Seen, true, ct);
        }
        await client.DisconnectAsync(true, ct);
    }

    private string Required(string key) => config[$"LeadIntake:IndiaMart:{key}"] is { Length: > 0 } value ? value : throw new InvalidOperationException($"LeadIntake:IndiaMart:{key} is required.");
    private static string StripHtml(string html) => System.Net.WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " "));
}
