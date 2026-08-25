using DgSales.Api.Application;
using Xunit;

namespace DgSales.Domain.Tests;

public sealed class OwnerQuotationReplyParserTests
{
    private readonly OwnerQuotationReplyParser parser = new();

    [Fact]
    public void ParsesPricingAndRemovesIndianNumberSeparators()
    {
        var reply = parser.Parse("PRICE VSS-ABC123 SELLING 5,00,000 TRANSPORT 15,000 INSTALLATION 0");
        Assert.Equal(OwnerQuotationReplyKind.Pricing, reply.Kind);
        Assert.Equal("VSS-ABC123", reply.RequestCode);
        Assert.Equal(500000m, reply.SellingPrice);
        Assert.Equal(15000m, reply.TransportCharge);
        Assert.Equal(0m, reply.InstallationCharge);
    }

    [Theory]
    [InlineData("APPROVE VSS-ABC123", OwnerQuotationReplyKind.Approve)]
    [InlineData("reject vss-abc123 transport too high", OwnerQuotationReplyKind.Reject)]
    public void ParsesDecisionWithRequestCode(string text, OwnerQuotationReplyKind expected)
    {
        var reply = parser.Parse(text);
        Assert.Equal(expected, reply.Kind);
        Assert.Equal("VSS-ABC123", reply.RequestCode);
    }

    [Fact]
    public void RejectsAmbiguousFreeText() =>
        Assert.Equal(OwnerQuotationReplyKind.Unknown, parser.Parse("price is around five lakh").Kind);
}

