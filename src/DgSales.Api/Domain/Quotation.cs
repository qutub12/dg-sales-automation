namespace DgSales.Api.Domain;

public enum QuotationStatus { Generated, AwaitingOwnerApproval, Approved, Sent, ReviewRequired, Superseded }

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
    public decimal SellingPrice { get; private set; }
    public decimal TransportCharge { get; private set; }
    public decimal InstallationCharge { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal GstAmount { get; private set; }
    public decimal GrandTotal { get; private set; }
    public QuotationStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    public static Quotation Generate(
        Guid leadId, Guid requirementId, string priceVersion, string brand, string gensetModel,
        decimal kva, int phaseCount, decimal sellingPrice, decimal transportCharge,
        decimal installationCharge, decimal subtotal, decimal gstAmount, decimal grandTotal) => new()
    {
        LeadId = leadId,
        RequirementId = requirementId,
        QuotationNumber = $"DG-{DateTimeOffset.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
        PriceVersion = priceVersion,
        Brand = brand,
        GensetModel = gensetModel,
        Kva = kva,
        PhaseCount = phaseCount,
        SellingPrice = sellingPrice,
        TransportCharge = transportCharge,
        InstallationCharge = installationCharge,
        Subtotal = subtotal,
        GstAmount = gstAmount,
        GrandTotal = grandTotal,
        Status = QuotationStatus.Generated
    };

    public void MarkSent() => Status = QuotationStatus.Sent;
    public void MarkAwaitingOwnerApproval() => Status = QuotationStatus.AwaitingOwnerApproval;
    public void MarkApproved()
    {
        if (Status != QuotationStatus.AwaitingOwnerApproval)
            throw new InvalidOperationException("Only a quotation awaiting owner approval can be approved.");
        Status = QuotationStatus.Approved;
    }
    public void MarkReviewRequired() => Status = QuotationStatus.ReviewRequired;
}
