namespace DgSales.Api.Application;

public sealed record StandardQuotationRequest(
    decimal BasePrice,
    decimal StandardMarkup,
    decimal TransportCharge,
    decimal InstallationCharge,
    decimal AccessoryCharge,
    decimal GstPercent);

public sealed record StandardQuotationResult(decimal Subtotal, decimal GstAmount, decimal GrandTotal, bool IsValid, IReadOnlyCollection<string> Errors);

public sealed class StandardQuotationCalculator
{
    public StandardQuotationResult Calculate(StandardQuotationRequest request)
    {
        var errors = new List<string>();
        if (request.BasePrice <= 0) errors.Add("Base price must be greater than zero.");
        if (request.StandardMarkup < 0 || request.TransportCharge < 0 || request.InstallationCharge < 0 || request.AccessoryCharge < 0)
            errors.Add("Standard charges cannot be negative.");
        if (request.GstPercent is < 0 or > 100) errors.Add("GST percent must be between zero and 100.");
        if (errors.Count > 0) return new(0, 0, 0, false, errors);

        var subtotal = request.BasePrice + request.StandardMarkup + request.TransportCharge + request.InstallationCharge + request.AccessoryCharge;
        var gst = decimal.Round(subtotal * request.GstPercent / 100, 2, MidpointRounding.AwayFromZero);
        return new(subtotal, gst, subtotal + gst, true, errors);
    }
}
