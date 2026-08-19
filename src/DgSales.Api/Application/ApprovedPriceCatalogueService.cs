using System.Text.Json;
using DgSales.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DgSales.Api.Application;

public sealed record ApprovedPriceItem(
    string Version,
    string Brand,
    string GensetModel,
    decimal Kva,
    int PhaseCount,
    decimal BasePrice,
    decimal StandardMarkup,
    decimal TransportCharge,
    decimal InstallationCharge,
    decimal AccessoryCharge,
    decimal GstPercent,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsActive);

public sealed class ApprovedPriceCatalogueService(IConfiguration configuration, TimeProvider timeProvider, SalesDbContext db)
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public ApprovedPriceItem? Find(decimal kva, int phaseCount, string? preferredBrand)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var databaseItem = db.PriceCatalogueEntries.AsNoTracking()
            .Where(x => x.IsActive && x.Kva == kva && x.PhaseCount == phaseCount)
            .Where(x => x.EffectiveFrom <= today && (x.EffectiveTo == null || x.EffectiveTo >= today))
            .Where(x => string.IsNullOrWhiteSpace(preferredBrand) || x.Brand.ToLower() == preferredBrand.Trim().ToLower())
            .OrderByDescending(x => x.EffectiveFrom).ThenByDescending(x => x.CreatedAtUtc).FirstOrDefault();
        if (databaseItem is not null) return new(databaseItem.Version, databaseItem.Brand, databaseItem.GensetModel,
            databaseItem.Kva, databaseItem.PhaseCount, databaseItem.BasePrice, databaseItem.StandardMarkup,
            databaseItem.TransportCharge, databaseItem.InstallationCharge, databaseItem.AccessoryCharge,
            databaseItem.GstPercent, databaseItem.EffectiveFrom, databaseItem.EffectiveTo, databaseItem.IsActive);

        var path = configuration["Pricing:ApprovedCataloguePath"];
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;

        using var stream = File.OpenRead(path);
        var items = JsonSerializer.Deserialize<ApprovedPriceItem[]>(stream, _jsonOptions) ?? [];
        return items
            .Where(x => x.IsActive && x.Kva == kva && x.PhaseCount == phaseCount)
            .Where(x => x.EffectiveFrom <= today && (x.EffectiveTo is null || x.EffectiveTo >= today))
            .Where(x => string.IsNullOrWhiteSpace(preferredBrand)
                || string.Equals(x.Brand, preferredBrand.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.EffectiveFrom)
            .ThenByDescending(x => x.Version, StringComparer.Ordinal)
            .FirstOrDefault();
    }
}
