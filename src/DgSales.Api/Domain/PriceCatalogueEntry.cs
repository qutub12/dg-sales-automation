namespace DgSales.Api.Domain;

public sealed record SavePriceRequest(string Brand, string GensetModel, decimal Kva, int PhaseCount, decimal BasePrice,
    decimal StandardMarkup, decimal TransportCharge, decimal InstallationCharge, decimal AccessoryCharge,
    decimal GstPercent, DateOnly EffectiveFrom, string ChangeReason);

public sealed class PriceCatalogueEntry
{
    private PriceCatalogueEntry() { }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Version { get; private set; } = string.Empty;
    public string Brand { get; private set; } = string.Empty;
    public string GensetModel { get; private set; } = string.Empty;
    public decimal Kva { get; private set; }
    public int PhaseCount { get; private set; }
    public decimal BasePrice { get; private set; }
    public decimal StandardMarkup { get; private set; }
    public decimal TransportCharge { get; private set; }
    public decimal InstallationCharge { get; private set; }
    public decimal AccessoryCharge { get; private set; }
    public decimal GstPercent { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string ChangeReason { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    public static PriceCatalogueEntry Create(SavePriceRequest x)
    {
        if (string.IsNullOrWhiteSpace(x.Brand) || string.IsNullOrWhiteSpace(x.GensetModel)) throw new ArgumentException("Brand and model are required.");
        if (x.Kva <= 0 || x.PhaseCount is not 1 and not 3 || x.BasePrice <= 0 || x.GstPercent is < 0 or > 100) throw new ArgumentException("Price values are invalid.");
        if (string.IsNullOrWhiteSpace(x.ChangeReason)) throw new ArgumentException("Change reason is required.");
        return new() { Version = $"P-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6]}", Brand = x.Brand.Trim(), GensetModel = x.GensetModel.Trim(), Kva = x.Kva, PhaseCount = x.PhaseCount, BasePrice = x.BasePrice, StandardMarkup = x.StandardMarkup, TransportCharge = x.TransportCharge, InstallationCharge = x.InstallationCharge, AccessoryCharge = x.AccessoryCharge, GstPercent = x.GstPercent, EffectiveFrom = x.EffectiveFrom, ChangeReason = x.ChangeReason.Trim() };
    }
    public void Deactivate(DateOnly effectiveTo) { IsActive = false; EffectiveTo = effectiveTo; }
}
