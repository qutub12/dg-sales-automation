using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace DgSales.Api.Integrations.WhatsApp;

public sealed class MetaWhatsAppProvider(HttpClient httpClient, IConfiguration configuration) : IWhatsAppProvider
{
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
                components = new[]
                {
                    new
                    {
                        type = "header",
                        parameters = new[]
                        {
                            new { type = "document", document = new { link = request.DocumentUri.ToString(), filename = request.FileName } }
                        }
                    }
                }
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

    private string Required(string key) => string.IsNullOrWhiteSpace(configuration[key])
        ? throw new InvalidOperationException($"{key} is required when WhatsApp delivery is enabled.")
        : configuration[key]!;
}
