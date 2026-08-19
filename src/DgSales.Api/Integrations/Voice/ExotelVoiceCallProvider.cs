using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DgSales.Api.Integrations.Voice;

public sealed class ExotelVoiceCallProvider(HttpClient httpClient, IConfiguration configuration) : IVoiceCallProvider
{
    public async Task<StartVoiceCallResult> StartAsync(StartVoiceCallRequest request, CancellationToken cancellationToken)
    {
        var accountSid = Required("Voice:Exotel:AccountSid");
        var apiKey = Required("Voice:Exotel:ApiKey");
        var apiToken = Required("Voice:Exotel:ApiToken");
        var callerId = Required("Voice:Exotel:CallerId");
        var flowUrl = Required("Voice:Exotel:FlowUrl");
        var regionHost = configuration["Voice:Exotel:ApiHost"] ?? "api.in.exotel.com";
        if (regionHost is not ("api.in.exotel.com" or "api.exotel.com"))
            throw new InvalidOperationException("Voice:Exotel:ApiHost must be an official Exotel API host.");
        if (!Uri.TryCreate(flowUrl, UriKind.Absolute, out var flow) || flow.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Voice:Exotel:FlowUrl must be the HTTPS URL of an active Exotel flow.");

        using var message = new HttpRequestMessage(
            HttpMethod.Post, $"https://{regionHost}/v1/Accounts/{Uri.EscapeDataString(accountSid)}/Calls/connect");
        message.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:{apiToken}")));
        message.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["From"] = request.CustomerPhone,
            ["CallerId"] = callerId,
            ["Url"] = flow.ToString(),
            ["CallType"] = "trans",
            ["TimeLimit"] = (configuration.GetValue<int?>("Voice:Exotel:TimeLimitSeconds") ?? 600).ToString(),
            ["TimeOut"] = (configuration.GetValue<int?>("Voice:Exotel:RingTimeoutSeconds") ?? 30).ToString(),
            ["StatusCallback"] = request.StatusWebhookUri.ToString(),
            ["StatusCallbackEvents"] = "terminal",
            ["StatusCallbackContentType"] = "application/json",
            ["CustomField"] = request.CallJobId.ToString("N")
        });

        using var response = await httpClient.SendAsync(message, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Exotel returned {(int)response.StatusCode}: {body}", null, response.StatusCode);

        using var json = JsonDocument.Parse(body);
        var call = GetProperty(json.RootElement, "call");
        var sid = GetString(call, "sid") ?? throw new InvalidOperationException("Exotel response did not contain a call SID.");
        return new(sid, GetString(call, "status") ?? "accepted");
    }

    private static JsonElement GetProperty(JsonElement value, string name) =>
        value.EnumerateObject().First(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)).Value;

    private static string? GetString(JsonElement value, string name)
    {
        foreach (var property in value.EnumerateObject())
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)) return property.Value.GetString();
        return null;
    }

    private string Required(string key) => string.IsNullOrWhiteSpace(configuration[key])
        ? throw new InvalidOperationException($"{key} is required when Exotel voice calls are enabled.")
        : configuration[key]!;
}
