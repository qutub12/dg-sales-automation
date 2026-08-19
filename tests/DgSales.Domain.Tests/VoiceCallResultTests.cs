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
}
