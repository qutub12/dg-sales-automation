namespace DgSales.Api.Domain;

public enum VoiceCallOutcome { Completed, NoAnswer, Busy, Failed, CustomerRequestedCallback, Escalated }

public sealed class VoiceCallResult
{
    private VoiceCallResult() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CallJobId { get; private set; }
    public Guid LeadId { get; private set; }
    public string ProviderCallId { get; private set; } = string.Empty;
    public VoiceCallOutcome Outcome { get; private set; }
    public PreferredLanguage DetectedLanguage { get; private set; }
    public bool AutomationDisclosed { get; private set; }
    public bool RecordingConsentGiven { get; private set; }
    public string? Transcript { get; private set; }
    public DateTimeOffset CompletedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    public static VoiceCallResult Capture(
        Guid callJobId, Guid leadId, string providerCallId, VoiceCallOutcome outcome,
        PreferredLanguage language, bool automationDisclosed, bool recordingConsentGiven, string? transcript) => new()
    {
        CallJobId = callJobId,
        LeadId = leadId,
        ProviderCallId = providerCallId,
        Outcome = outcome,
        DetectedLanguage = language,
        AutomationDisclosed = automationDisclosed,
        RecordingConsentGiven = recordingConsentGiven,
        Transcript = string.IsNullOrWhiteSpace(transcript) ? null : transcript.Trim()
    };
}
