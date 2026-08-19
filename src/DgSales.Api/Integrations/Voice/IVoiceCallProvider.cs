namespace DgSales.Api.Integrations.Voice;

public sealed record StartVoiceCallRequest(
    Guid CallJobId, Guid LeadId, string CustomerPhone, string CustomerName, string? City,
    string LanguageHint, Uri StatusWebhookUri, string AgentInstructions);
public sealed record StartVoiceCallResult(string ProviderCallId, string Status);

public interface IVoiceCallProvider
{
    Task<StartVoiceCallResult> StartAsync(StartVoiceCallRequest request, CancellationToken cancellationToken);
}
