using DgSales.Api.Domain;
using Xunit;

namespace DgSales.Domain.Tests;

public sealed class LeadTests
{
    [Theory]
    [InlineData("9876543210", "+919876543210")]
    [InlineData("+91 98765 43210", "+919876543210")]
    public void NormalizesIndianPhoneNumbers(string input, string expected) =>
        Assert.Equal(expected, Lead.NormalizePhone(input));

    [Fact]
    public void NewLeadCanBeQueuedForCall()
    {
        var lead = Lead.Create(new CreateLeadRequest("Customer", "9876543210", "Nagpur", LeadSource.Manual, null));
        lead.QueueCall();
        Assert.Equal(LeadStatus.CallQueued, lead.Status);
    }

    [Fact]
    public void LeadCannotBeQueuedTwice()
    {
        var lead = Lead.Create(new CreateLeadRequest("Customer", "9876543210", "Nagpur", LeadSource.Manual, null));
        lead.QueueCall();
        Assert.Throws<InvalidOperationException>(lead.QueueCall);
    }

    [Fact]
    public void QualifiedLeadCanMoveToQuotationOrEscalation()
    {
        var quoted = Lead.Create(new CreateLeadRequest("Customer", "9876543210", "Nagpur", LeadSource.Manual, null));
        quoted.MarkQualified();
        quoted.MarkQuotationPending();
        Assert.Equal(LeadStatus.QuotationPending, quoted.Status);

        var escalated = Lead.Create(new CreateLeadRequest("Customer", "9876543211", "Nagpur", LeadSource.Manual, null));
        escalated.MarkQualified();
        escalated.MarkEscalated();
        Assert.Equal(LeadStatus.Escalated, escalated.Status);
    }
}
