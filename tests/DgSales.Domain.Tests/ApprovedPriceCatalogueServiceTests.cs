using DgSales.Api.Application;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DgSales.Domain.Tests;

public sealed class ApprovedPriceCatalogueServiceTests
{
    [Fact]
    public void MissingPrivateCatalogueNeverReturnsAnApprovedPrice()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var service = new ApprovedPriceCatalogueService(configuration, TimeProvider.System);

        Assert.Null(service.Find(25, 3, null));
    }

    [Fact]
    public void SelectsOnlyActiveEffectiveMatchingPrice()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, """
                [{
                  "version":"approved-2026-08", "brand":"Kirloskar", "gensetModel":"KG4-25",
                  "kva":25, "phaseCount":3, "basePrice":100, "standardMarkup":10,
                  "transportCharge":5, "installationCharge":0, "accessoryCharge":0,
                  "gstPercent":18, "effectiveFrom":"2026-08-01", "effectiveTo":null, "isActive":true
                }]
                """);
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Pricing:ApprovedCataloguePath"] = path })
                .Build();
            var service = new ApprovedPriceCatalogueService(configuration, new FixedTimeProvider(new DateTimeOffset(2026, 8, 19, 0, 0, 0, TimeSpan.Zero)));

            var price = service.Find(25, 3, "kirloskar");

            Assert.NotNull(price);
            Assert.Equal("approved-2026-08", price.Version);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
