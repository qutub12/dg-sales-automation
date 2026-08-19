using DgSales.Api.Application;
using Xunit;

namespace DgSales.Domain.Tests;

public sealed class LeadMessageParserTests
{
    private readonly LeadMessageParser parser = new();

    [Fact]
    public void ParsesLabelledIndiaMartLead()
    {
        var result = parser.Parse("Name: Test Buyer\nMobile: +91 98765-43210\nCity: Nagpur\nRequirement: 30 kVA generator\nEnquiry ID: IM-123");
        Assert.Equal("Test Buyer", result.CustomerName);
        Assert.Equal("9876543210", result.Phone);
        Assert.Equal("Nagpur", result.City);
        Assert.Equal("IM-123", result.SourceReference);
    }

    [Fact]
    public void DoesNotGuessWhenTwoPhonesArePresent()
    {
        var result = parser.Parse("Please call 9876543210 or 9123456789 regarding a generator.");
        Assert.Null(result.Phone);
        Assert.Contains("PhoneMissingOrAmbiguous", result.Flags);
    }
}
