namespace DgSales.Api.Application;

public sealed record QuotationEligibilityRequest(
    bool RequirementComplete,
    bool SizingConfirmed,
    bool ActiveCatalogueItemFound,
    bool ApprovedPriceFound,
    bool CustomDiscountRequested,
    bool NonStandardTermsRequested,
    bool StockOrDeliveryPromiseRequired,
    bool HasValidationFlags);

public sealed record QuotationEligibilityResult(bool CanSendAutomatically, IReadOnlyCollection<string> ReviewReasons);

public sealed class QuotationEligibilityService
{
    private readonly IConfiguration? configuration;
    public QuotationEligibilityService(IConfiguration? configuration = null) => this.configuration = configuration;
    public QuotationEligibilityResult Assess(QuotationEligibilityRequest request)
    {
        var reasons = new List<string>();
        if (!request.RequirementComplete) reasons.Add("Customer requirement is incomplete.");
        if (!request.SizingConfirmed) reasons.Add("Generator sizing is not confirmed.");
        if (!request.ActiveCatalogueItemFound) reasons.Add("No active catalogue item matches the requirement.");
        if (!request.ApprovedPriceFound) reasons.Add("No approved price is available.");
        if (request.CustomDiscountRequested) reasons.Add("Customer requested a custom discount.");
        if (request.NonStandardTermsRequested) reasons.Add("Customer requested non-standard commercial terms.");
        if (request.StockOrDeliveryPromiseRequired) reasons.Add("Stock or delivery requires owner confirmation.");
        if (request.HasValidationFlags) reasons.Add("Requirement contains validation or confidence flags.");
        if (configuration?.GetValue<bool>("Pricing:RequireOwnerApprovalForEveryQuotation") == true)
            reasons.Add("Owner must confirm price, transport, installation requirement and delivery before sending.");
        return new(reasons.Count == 0, reasons);
    }
}
