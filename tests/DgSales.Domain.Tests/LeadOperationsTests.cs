using DgSales.Api.Domain;
using Xunit;

namespace DgSales.Domain.Tests;

public sealed class LeadOperationsTests
{
    [Fact]
    public void ContactRestrictionCanBeAppliedAndRemoved()
    {
        var lead = Lead.Create(new("Customer", "9876543210", "Nagpur", LeadSource.Manual, null));
        lead.RestrictContact("Requested no calls"); Assert.False(lead.ContactAllowed);
        lead.AllowContact(); Assert.True(lead.ContactAllowed);
    }

    [Fact]
    public void AutomationCanBePausedAndResumed()
    {
        var control = AutomationControl.Create("calls"); control.Set(true, "Maintenance"); Assert.True(control.IsPaused);
        control.Set(false, null); Assert.False(control.IsPaused);
    }
}
