using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace DgSales.Api.Integrations.WhatsApp;

public sealed class MetaWhatsAppProvider(HttpClient httpClient, IConfiguration configuration) : IWhatsAppProvider
{
    public async Task<SendWhatsAppResult> SendTemplateAsync(SendWhatsAppTemplateRequest request, CancellationToken cancellationToken)
    {
        var components = request.BodyParameters.Count == 0 ? null : new[] { new { type = "body", parameters = request.BodyParameters.Select(x => new { type = "text", text = x }).ToArray() } };
        return await SendAsync(request.Phone, new { name = request.TemplateName, language = new { code = request.LanguageCode }, components }, cancellationToken);
    }

    public async Task<SendWhatsAppResult> SendDocumentAsync(SendWhatsAppDocumentRequest request, CancellationToken cancellationToken)
    {
        var phoneNumberId = Required("WhatsApp:PhoneNumberId");
        var accessToken = Required("WhatsApp:AccessToken");
        var apiVersion = Required("WhatsApp:GraphApiVersion");
        using var message = new HttpRequestMessage(HttpMethod.Post, $"{apiVersion}/{phoneNumberId}/messages");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        message.Content = JsonContent.Create(new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to = request.CustomerPhone.TrimStart('+'),
            type = "template",
            template = new
            {
                name = request.TemplateName,
                language = new { code = request.LanguageCode },
                components = BuildDocumentComponents(request)
            }
        });

        using var response = await httpClient.SendAsync(message, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"WhatsApp API returned {(int)response.StatusCode}: {body}", null, response.StatusCode);

        using var json = JsonDocument.Parse(body);
        var id = json.RootElement.GetProperty("messages")[0].GetProperty("id").GetString();
        return new(id ?? throw new InvalidOperationException("WhatsApp response did not contain a message id."), "accepted");
    }

    private static object[] BuildDocumentComponents(SendWhatsAppDocumentRequest request)
    {
        var components = new List<object>
        {
            new { type = "header", parameters = new[] { new { type = "document", document = new { link = request.DocumentUri.ToString(), filename = request.FileName } } } }
        };
        if (request.BodyParameters is { Count: > 0 })
            components.Add(new { type = "body", parameters = request.BodyParameters.Select(x => new { type = "text", text = x }).ToArray() });
        return components.ToArray();
    }

    private async Task<SendWhatsAppResult> SendAsync(string phone, object template, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, $"{Required("WhatsApp:GraphApiVersion")}/{Required("WhatsApp:PhoneNumberId")}/messages");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Required("WhatsApp:AccessToken"));
        message.Content = JsonContent.Create(new { messaging_product = "whatsapp", recipient_type = "individual", to = phone.TrimStart('+'), type = "template", template });
        using var response = await httpClient.SendAsync(message, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"WhatsApp API returned {(int)response.StatusCode}: {body}", null, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        return new(json.RootElement.GetProperty("messages")[0].GetProperty("id").GetString() ?? throw new InvalidOperationException("WhatsApp response did not contain a message id."), "accepted");
    }

    private string Required(string key) => string.IsNullOrWhiteSpace(configuration[key])
        ? throw new InvalidOperationException($"{key} is required when WhatsApp delivery is enabled.")
        : configuration[key]!;
}
