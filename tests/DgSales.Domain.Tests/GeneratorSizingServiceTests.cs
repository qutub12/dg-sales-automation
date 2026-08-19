using DgSales.Api.Application;
using Xunit;

namespace DgSales.Domain.Tests;

public sealed class GeneratorSizingServiceTests
{
    private readonly GeneratorSizingService _service = new();

    [Fact]
    public void AddsLargestStartingLoadAndSafetyMargin()
    {
        var result = _service.Calculate(new SizingRequest([
            new LoadItem("Motor", 1, 10m, 3m),
            new LoadItem("Lighting", 1, 5m)
        ]));

        Assert.Equal(15m, result.RunningKw);
        Assert.Equal(35m, result.PeakKw);
        Assert.Equal(52.5m, result.RequiredKva);
        Assert.Equal(62, result.RecommendedKva);
        Assert.False(result.RequiresReview);
    }

    [Fact]
    public void RejectsInvalidLoadInsteadOfGuessing()
    {
        var result = _service.Calculate(new SizingRequest([new LoadItem("Unknown", 0, 0)]));
        Assert.True(result.RequiresReview);
        Assert.Null(result.RecommendedKva);
    }

    [Fact]
    public void ConvertsHorsepowerAndAppliesMotorStartingRule()
    {
        var result = _service.Calculate(new SizingRequest([
            new LoadItem("Pump", 1, 0, InputUnit: LoadInputUnit.Hp, InputValue: 10, Kind: LoadKind.MotorDirectOnLine, Efficiency: 1m)
        ], SafetyMarginPercent: 0));
        Assert.Equal(7.46m, result.RunningKw);
        Assert.Equal(27.98m, result.RequiredKva);
        Assert.Equal(30, result.RecommendedKva);
    }
}
