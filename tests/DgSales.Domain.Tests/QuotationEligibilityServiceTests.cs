using DgSales.Api.Application;
using Xunit;

namespace DgSales.Domain.Tests;

public sealed class QuotationEligibilityServiceTests
{
    private readonly QuotationEligibilityService _service = new();

    [Fact]
    public void AllowsOnlyFullyStandardQuotation()
    {
        var result = _service.Assess(new(true, true, true, true, false, false, false, false));
        Assert.True(result.CanSendAutomatically);
        Assert.Empty(result.ReviewReasons);
    }

    [Fact]
    public void EscalatesDiscountAndDeliveryPromise()
    {
        var result = _service.Assess(new(true, true, true, true, true, false, true, false));
        Assert.False(result.CanSendAutomatically);
        Assert.Equal(2, result.ReviewReasons.Count);
    }
}
