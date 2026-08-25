using DgSales.Api.Domain;
using Xunit;

namespace DgSales.Domain.Tests;

public sealed class OwnerQuotationApprovalTests
{
    [Fact]
    public void RequiresPricingBeforeApproval()
    {
        var request = OwnerQuotationApproval.Queue(Guid.NewGuid(), Guid.NewGuid(), "Cummins", "C125", 125, 3, false);
        Assert.Throws<InvalidOperationException>(() => request.Approve("APPROVE VSS-ABC123"));
        request.MarkPricingRequestSent("wamid.1");
        request.SupplyPricing(500000, 15000, 0, "price reply");
        request.AttachQuotation(Guid.NewGuid());
        request.MarkApprovalRequestSent("wamid.2");
        request.Approve($"APPROVE {request.RequestCode}");
        Assert.Equal(OwnerQuotationApprovalStatus.Approved, request.Status);
    }

    [Fact]
    public void FailedPricingRequestCanBeRetriedWithoutChangingItsCode()
    {
        var request = OwnerQuotationApproval.Queue(Guid.NewGuid(), Guid.NewGuid(), "Cummins", "C125", 125, 3, false);
        var code = request.RequestCode;
        request.MarkFailed("provider unavailable", false, DateTimeOffset.UtcNow);
        request.Retry(DateTimeOffset.UtcNow.AddMinutes(1));
        Assert.Equal(OwnerQuotationApprovalStatus.PricingRequestQueued, request.Status);
        Assert.Equal(code, request.RequestCode);
        Assert.Null(request.LastError);
    }
}
