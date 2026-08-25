namespace DgSales.Api.Domain;

public enum OwnerQuotationApprovalStatus
{
    PricingRequestQueued,
    AwaitingPricing,
    ApprovalRequestQueued,
    AwaitingApproval,
    Approved,
    Rejected,
    Failed
}

public sealed class OwnerQuotationApproval
{
    private OwnerQuotationApproval() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid LeadId { get; private set; }
    public Guid RequirementId { get; private set; }
    public Guid? QuotationId { get; private set; }
    public string RequestCode { get; private set; } = string.Empty;
    public string Brand { get; private set; } = string.Empty;
    public string GensetModel { get; private set; } = string.Empty;
    public decimal Kva { get; private set; }
    public int PhaseCount { get; private set; }
    public bool InstallationRequired { get; private set; }
    public decimal? SellingPrice { get; private set; }
    public decimal? TransportCharge { get; private set; }
    public decimal? InstallationCharge { get; private set; }
    public decimal GstPercent { get; private set; } = 18m;
    public OwnerQuotationApprovalStatus Status { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public string? OwnerReply { get; private set; }
    public string? LastError { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTimeOffset ScheduledAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public uint Version { get; private set; }

    public static OwnerQuotationApproval Queue(Guid leadId, Guid requirementId, string brand,
        string gensetModel, decimal kva, int phaseCount, bool installationRequired, decimal gstPercent = 18m) => new()
    {
        LeadId = leadId,
        RequirementId = requirementId,
        RequestCode = $"VSS-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
        Brand = brand.Trim(),
        GensetModel = gensetModel.Trim(),
        Kva = kva,
        PhaseCount = phaseCount,
        InstallationRequired = installationRequired,
        GstPercent = gstPercent,
        Status = OwnerQuotationApprovalStatus.PricingRequestQueued
    };

    public void MarkPricingRequestSent(string providerMessageId)
    {
        ProviderMessageId = providerMessageId;
        Status = OwnerQuotationApprovalStatus.AwaitingPricing;
        LastError = null;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void SupplyPricing(decimal sellingPrice, decimal transportCharge, decimal installationCharge, string reply)
    {
        if (Status != OwnerQuotationApprovalStatus.AwaitingPricing)
            throw new InvalidOperationException("This request is not awaiting pricing.");
        if (sellingPrice <= 0 || transportCharge < 0 || installationCharge < 0)
            throw new ArgumentException("Selling price must be positive and charges cannot be negative.");
        SellingPrice = sellingPrice;
        TransportCharge = transportCharge;
        InstallationCharge = installationCharge;
        OwnerReply = reply;
        Status = OwnerQuotationApprovalStatus.ApprovalRequestQueued;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void AttachQuotation(Guid quotationId) => QuotationId = quotationId;

    public void MarkApprovalRequestSent(string providerMessageId)
    {
        ProviderMessageId = providerMessageId;
        Status = OwnerQuotationApprovalStatus.AwaitingApproval;
        LastError = null;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Approve(string reply)
    {
        if (Status != OwnerQuotationApprovalStatus.AwaitingApproval)
            throw new InvalidOperationException("This request is not awaiting approval.");
        OwnerReply = reply;
        Status = OwnerQuotationApprovalStatus.Approved;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Reject(string reply)
    {
        if (Status != OwnerQuotationApprovalStatus.AwaitingApproval)
            throw new InvalidOperationException("This request is not awaiting approval.");
        OwnerReply = reply;
        Status = OwnerQuotationApprovalStatus.Rejected;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkSending() { AttemptCount++; UpdatedAtUtc = DateTimeOffset.UtcNow; }
    public void MarkFailed(string error, bool retry, DateTimeOffset retryAtUtc)
    {
        LastError = error.Length > 1000 ? error[..1000] : error;
        if (retry) ScheduledAtUtc = retryAtUtc; else Status = OwnerQuotationApprovalStatus.Failed;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
