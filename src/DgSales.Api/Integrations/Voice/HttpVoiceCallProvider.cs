using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace DgSales.Api.Integrations.Voice;

public sealed class HttpVoiceCallProvider(HttpClient httpClient, IConfiguration configuration) : IVoiceCallProvider
{
    public async Task<StartVoiceCallResult> StartAsync(StartVoiceCallRequest request, CancellationToken cancellationToken)
    {
        var endpoint = configuration["Voice:StartCallEndpoint"];
        var token = configuration["Voice:ApiToken"];
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
            throw new InvalidOperationException("Voice:StartCallEndpoint must be an absolute HTTPS URL.");
        if (uri.Scheme != Uri.UriSchemeHttps) throw new InvalidOperationException("Voice call endpoint must use HTTPS.");
        if (string.IsNullOrWhiteSpace(token)) throw new InvalidOperationException("Voice:ApiToken is required.");

        using var message = new HttpRequestMessage(HttpMethod.Post, uri);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        message.Content = JsonContent.Create(request);
        using var response = await httpClient.SendAsync(message, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Voice provider returned {(int)response.StatusCode}: {body}", null, response.StatusCode);
        return JsonSerializer.Deserialize<StartVoiceCallResult>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Voice provider returned an empty response.");
    }
}
