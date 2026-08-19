using DgSales.Api.Domain;
using Xunit;

namespace DgSales.Domain.Tests;

public sealed class CustomerRequirementTests
{
    [Fact]
    public void CompleteStandardRequirementCanProceedToEligibilityCheck()
    {
        var requirement = CustomerRequirement.Capture(Guid.NewGuid(), new(
            25, 3, "Kirloskar", "Construction backup", "Nagpur",
            true, false, false, false, []));

        Assert.True(requirement.IsComplete);
        Assert.True(requirement.SizingConfirmed);
        Assert.False(requirement.HasValidationFlags);
    }

    [Fact]
    public void VoiceValidationFlagsAreTrimmedAndDeduplicated()
    {
        var requirement = CustomerRequirement.Capture(Guid.NewGuid(), new(
            25, 3, null, "Motor", "Nagpur",
            true, false, false, false, [" uncertain load ", "uncertain load"]));

        Assert.Single(requirement.ValidationFlags);
        Assert.Equal("uncertain load", requirement.ValidationFlags.Single());
        Assert.True(requirement.HasValidationFlags);
    }
}
