using DgSales.Api.Application;
using Xunit;

namespace DgSales.Domain.Tests;

public sealed class StandardQuotationCalculatorTests
{
    [Fact]
    public void CalculatesOnlyConfiguredChargesAndGst()
    {
        var result = new StandardQuotationCalculator().Calculate(new(100000m, 10000m, 5000m, 2000m, 3000m, 18m));
        Assert.True(result.IsValid);
        Assert.Equal(120000m, result.Subtotal);
        Assert.Equal(21600m, result.GstAmount);
        Assert.Equal(141600m, result.GrandTotal);
    }

    [Fact]
    public void RejectsNegativeCommercialValues()
    {
        var result = new StandardQuotationCalculator().Calculate(new(100000m, -1m, 0, 0, 0, 18m));
        Assert.False(result.IsValid);
    }
}
