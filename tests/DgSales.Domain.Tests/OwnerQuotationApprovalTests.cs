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
}
