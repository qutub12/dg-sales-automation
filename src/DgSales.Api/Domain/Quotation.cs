namespace DgSales.Api.Domain;

public enum QuotationStatus { Generated, Sent, ReviewRequired, Superseded }

public sealed class Quotation
{
    private Quotation() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid LeadId { get; private set; }
    public Guid RequirementId { get; private set; }
    public string QuotationNumber { get; private set; } = string.Empty;
    public string PriceVersion { get; private set; } = string.Empty;
    public string Brand { get; private set; } = string.Empty;
    public string GensetModel { get; private set; } = string.Empty;
    public decimal Kva { get; private set; }
    public int PhaseCount { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal GstAmount { get; private set; }
    public decimal GrandTotal { get; private set; }
    public QuotationStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    public static Quotation Generate(
        Guid leadId, Guid requirementId, string priceVersion, string brand, string gensetModel,
        decimal kva, int phaseCount, decimal subtotal, decimal gstAmount, decimal grandTotal) => new()
    {
        LeadId = leadId,
        RequirementId = requirementId,
        QuotationNumber = $"DG-{DateTimeOffset.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
        PriceVersion = priceVersion,
        Brand = brand,
        GensetModel = gensetModel,
        Kva = kva,
        PhaseCount = phaseCount,
        Subtotal = subtotal,
        GstAmount = gstAmount,
        GrandTotal = grandTotal,
        Status = QuotationStatus.Generated
    };
}
