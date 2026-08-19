using DgSales.Api.Application;
using Xunit;

namespace DgSales.Domain.Tests;

public sealed class ServiceAreaMatcherTests
{
    private readonly ServiceAreaMatcher _matcher = new();

    [Theory]
    [InlineData("Nagpur")]
    [InlineData("Gondia, Maharashtra")]
    [InlineData("CHANDRAPUR")]
    public void RecognizesSupportedCities(string city) => Assert.True(_matcher.IsSupported(city));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Pune")]
    public void RejectsUnknownCities(string? city) => Assert.False(_matcher.IsSupported(city));
}
