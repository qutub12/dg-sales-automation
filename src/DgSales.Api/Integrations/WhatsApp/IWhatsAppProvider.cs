namespace DgSales.Api.Integrations.WhatsApp;

public sealed record SendWhatsAppDocumentRequest(Guid LeadId, string CustomerPhone, string TemplateName, string LanguageCode, Uri DocumentUri, string FileName);
public sealed record SendWhatsAppTemplateRequest(string Phone, string TemplateName, string LanguageCode, IReadOnlyList<string> BodyParameters);
public sealed record SendWhatsAppResult(string ProviderMessageId, string Status);

public interface IWhatsAppProvider
{
    Task<SendWhatsAppResult> SendDocumentAsync(SendWhatsAppDocumentRequest request, CancellationToken cancellationToken);
    Task<SendWhatsAppResult> SendTemplateAsync(SendWhatsAppTemplateRequest request, CancellationToken cancellationToken);
}
