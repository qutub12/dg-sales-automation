using System.Text.Json;
using DgSales.Api.Application;
using Xunit;

namespace DgSales.Domain.Tests;

public sealed class ReferenceCatalogueTests
{
    [Fact]
    public void TechnicalCatalogueContainsOnlyUsableRatings()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "data", "technical-catalogue-reference.json");
        var values = JsonSerializer.Deserialize<TechnicalCatalogueReference[]>(
            File.ReadAllText(path), new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

        Assert.NotEmpty(values);
        Assert.All(values, x =>
        {
            Assert.True(x.Kva > 0);
            Assert.True(x.Kwe > 0);
            Assert.Contains(x.PhaseCount, new[] { 1, 3 });
        });
    }
}
