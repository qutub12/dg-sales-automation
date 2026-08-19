using System.Security.Cryptography;
using System.Text;
using DgSales.Api.Application;
using DgSales.Api.Domain;
using Microsoft.Playwright;

namespace DgSales.Api.Integrations.Justdial;

public sealed class JustdialPortalWorker(
    IServiceScopeFactory scopes, IConfiguration config, ILogger<JustdialPortalWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var seconds = Math.Max(60, config.GetValue("LeadIntake:Justdial:PollSeconds", 300));
            if (config.GetValue<bool>("LeadIntake:Justdial:PortalEnabled"))
                try { await PollAsync(stoppingToken); }
                catch (PlaywrightException ex) { logger.LogError(ex, "Justdial portal automation failed. Check the saved session and configured selectors."); }
                catch (Exception ex) { logger.LogError(ex, "Justdial portal poll failed."); }
            await Task.Delay(TimeSpan.FromSeconds(seconds), stoppingToken);
        }
    }

    private async Task PollAsync(CancellationToken ct)
    {
        var portalUrl = Required("PortalUrl");
        var profilePath = Required("BrowserProfilePath");
        var cardsSelector = Required("Selectors:LeadCards");
        var authenticatedSelector = Required("Selectors:Authenticated");
        Directory.CreateDirectory(profilePath);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchPersistentContextAsync(profilePath, new()
        {
            Headless = config.GetValue("LeadIntake:Justdial:Headless", true),
            Channel = EmptyToNull(config["LeadIntake:Justdial:BrowserChannel"]),
            Args = ["--disable-dev-shm-usage"]
        });
        var page = browser.Pages.FirstOrDefault() ?? await browser.NewPageAsync();
        await page.GotoAsync(portalUrl, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        if (await page.Locator(authenticatedSelector).CountAsync() == 0)
        {
            if (config.GetValue("LeadIntake:Justdial:Headless", true))
                throw new InvalidOperationException("Justdial session is not authenticated. Run once with Headless=false to sign in.");
            logger.LogInformation("Waiting for manual Justdial login in the opened browser window.");
            await page.Locator(authenticatedSelector).WaitForAsync(new()
            {
                State = WaitForSelectorState.Visible,
                Timeout = TimeSpan.FromMinutes(Math.Max(1, config.GetValue("LeadIntake:Justdial:LoginBootstrapMinutes", 10))).TotalMilliseconds
            });
        }

        var cards = page.Locator(cardsSelector);
        var count = Math.Min(await cards.CountAsync(), Math.Max(1, config.GetValue("LeadIntake:Justdial:MaxLeadsPerPoll", 25)));
        for (var index = 0; index < count; index++)
        {
            var card = cards.Nth(index);
            var cardText = await card.InnerTextAsync();
            var stableKey = await StableKeyAsync(card, cardText);
            var body = await ReadLeadTextAsync(page, card);
            if (string.IsNullOrWhiteSpace(body)) continue;
            await using var scope = scopes.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<InboundLeadProcessor>().ProcessAsync(
                InboundLeadChannel.JustdialPortal, $"portal:{stableKey}", "Justdial portal", null, body, ct);
            if (page.Url != portalUrl) await page.GotoAsync(portalUrl, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        }
    }

    private async Task<string> ReadLeadTextAsync(IPage page, ILocator card)
    {
        var linkSelector = EmptyToNull(config["LeadIntake:Justdial:Selectors:DetailLink"]);
        if (linkSelector is null) return await card.InnerTextAsync();
        var link = card.Locator(linkSelector).First;
        if (await link.CountAsync() == 0) return await card.InnerTextAsync();
        var href = await link.GetAttributeAsync("href");
        if (!string.IsNullOrWhiteSpace(href))
            await page.GotoAsync(new Uri(new Uri(page.Url), href).ToString(), new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        else await link.ClickAsync();
        var detailsSelector = Required("Selectors:DetailContainer");
        await page.Locator(detailsSelector).WaitForAsync(new() { State = WaitForSelectorState.Visible });
        return await page.Locator(detailsSelector).First.InnerTextAsync();
    }

    private static async Task<string> StableKeyAsync(ILocator card, string body)
    {
        var sourceId = await card.GetAttributeAsync("data-lead-id") ?? await card.GetAttributeAsync("id") ?? body;
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sourceId))).ToLowerInvariant();
    }

    private string Required(string key) => config[$"LeadIntake:Justdial:{key}"] is { Length: > 0 } value
        ? value : throw new InvalidOperationException($"LeadIntake:Justdial:{key} is required.");
    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
