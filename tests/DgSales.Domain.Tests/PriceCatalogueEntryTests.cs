using DgSales.Api.Domain;
using Xunit;

namespace DgSales.Domain.Tests;

public sealed class PriceCatalogueEntryTests
{
    [Fact]
    public void RequiresReasonAndCreatesImmutableVersion()
    {
        var price = PriceCatalogueEntry.Create(new("Kirloskar", "Model X", 30, 3, 500000, 10000, 5000, 0, 0, 18, new DateOnly(2026, 8, 20), "Supplier revision"));
        Assert.StartsWith("P-", price.Version);
        Assert.True(price.IsActive);
        Assert.Throws<ArgumentException>(() => PriceCatalogueEntry.Create(new("Kirloskar", "Model X", 30, 3, 500000, 0, 0, 0, 0, 18, new DateOnly(2026, 8, 20), "")));
    }
}
