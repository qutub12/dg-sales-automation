using DgSales.Api.Domain;
using Xunit;

namespace DgSales.Domain.Tests;

public sealed class VoiceCallResultTests
{
    [Fact]
    public void CapturesDisclosureConsentAndLanguage()
    {
        var result = VoiceCallResult.Capture(
            Guid.NewGuid(), Guid.NewGuid(), "provider-1", VoiceCallOutcome.Completed,
            PreferredLanguage.Marathi, true, true, " confirmed requirement ");

        Assert.True(result.AutomationDisclosed);
        Assert.True(result.RecordingConsentGiven);
        Assert.Equal(PreferredLanguage.Marathi, result.DetectedLanguage);
        Assert.Equal("confirmed requirement", result.Transcript);
    }

    [Fact]
    public void CapturesCustomerRequestedCallbackTime()
    {
        var callback = new DateTimeOffset(2026, 8, 26, 7, 30, 0, TimeSpan.Zero);
        var result = VoiceCallResult.Capture(
            Guid.NewGuid(), Guid.NewGuid(), "provider-2", VoiceCallOutcome.CustomerRequestedCallback,
            PreferredLanguage.Hindi, true, false, null, callback);

        Assert.Equal(VoiceCallOutcome.CustomerRequestedCallback, result.Outcome);
        Assert.Equal(callback, result.RequestedCallbackAtUtc);
        Assert.False(result.RecordingConsentGiven);
    }
}
