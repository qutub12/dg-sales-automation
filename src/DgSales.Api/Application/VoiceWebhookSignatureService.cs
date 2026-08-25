using System.Security.Cryptography;
using System.Text;

namespace DgSales.Api.Application;

public sealed class VoiceWebhookSignatureService(IConfiguration configuration)
{
    public bool Validate(ReadOnlySpan<byte> body, string? signature)
    {
        var secret = configuration["Voice:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32 || string.IsNullOrWhiteSpace(signature)) return false;
        var expected = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), body)).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(signature));
    }
}

public sealed record VoiceCallResultWebhook(
    Guid CallJobId,
    string ProviderCallId,
    DgSales.Api.Domain.VoiceCallOutcome Outcome,
    DgSales.Api.Domain.PreferredLanguage DetectedLanguage,
    bool AutomationDisclosed,
    bool RecordingConsentGiven,
    string? Transcript,
    DgSales.Api.Domain.CaptureRequirementRequest? Requirement);
