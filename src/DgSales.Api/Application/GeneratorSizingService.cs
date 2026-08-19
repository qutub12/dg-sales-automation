namespace DgSales.Api.Application;

public enum LoadInputUnit { Kw, Hp, AmpereSinglePhase, AmpereThreePhase }
public enum LoadKind { Resistive, MotorDirectOnLine, MotorStarDelta, AirConditioner, Welding, Elevator, Other }
public sealed record LoadItem(string Name, int Quantity, decimal RunningKw, decimal StartingMultiplier = 1m,
    LoadInputUnit InputUnit = LoadInputUnit.Kw, decimal? InputValue = null, LoadKind Kind = LoadKind.Other,
    decimal Voltage = 415m, decimal Efficiency = 0.9m);
public sealed record SizingRequest(IReadOnlyCollection<LoadItem> Items, decimal PowerFactor = 0.8m, decimal SafetyMarginPercent = 20m);
public sealed record SizingResult(decimal RunningKw, decimal PeakKw, decimal RequiredKva, int? RecommendedKva, bool RequiresReview, IReadOnlyCollection<string> Reasons);

public sealed class GeneratorSizingService
{
    private static readonly int[] StandardCapacitiesKva = [5, 7, 10, 15, 20, 25, 30, 40, 50, 62, 75, 82, 100, 125, 160, 200, 250, 320, 400, 500, 625, 750, 1000];

    public SizingResult Calculate(SizingRequest request)
    {
        var reasons = new List<string>();
        if (request.Items.Count == 0) reasons.Add("At least one load item is required.");
        if (request.PowerFactor is <= 0 or > 1) reasons.Add("Power factor must be greater than zero and at most one.");
        if (request.SafetyMarginPercent is < 0 or > 100) reasons.Add("Safety margin must be between zero and 100 percent.");
        if (request.Items.Any(x => x.Quantity <= 0 || ResolveKw(x) <= 0 || ResolveMultiplier(x) < 1))
            reasons.Add("Every item requires positive quantity and running load, with starting multiplier of at least one.");
        if (reasons.Count > 0) return new(0, 0, 0, null, true, reasons);

        var runningKw = request.Items.Sum(x => x.Quantity * ResolveKw(x));
        var largestStartingAddition = request.Items.Max(x => ResolveKw(x) * (ResolveMultiplier(x) - 1));
        var peakKw = runningKw + largestStartingAddition;
        var requiredKva = decimal.Round((peakKw / request.PowerFactor) * (1 + request.SafetyMarginPercent / 100), 2, MidpointRounding.AwayFromZero);
        var recommendation = StandardCapacitiesKva.Cast<int?>().FirstOrDefault(x => x >= requiredKva);
        if (recommendation is null) reasons.Add("Required capacity is above the configured standard range.");

        return new(runningKw, peakKw, requiredKva, recommendation, recommendation is null, reasons);
    }

    private static decimal ResolveKw(LoadItem x) => x.InputValue is not { } value ? x.RunningKw : x.InputUnit switch
    {
        LoadInputUnit.Hp => value * 0.746m / x.Efficiency,
        LoadInputUnit.AmpereSinglePhase => x.Voltage * value / 1000m,
        LoadInputUnit.AmpereThreePhase => 1.732m * x.Voltage * value / 1000m,
        _ => value
    };
    private static decimal ResolveMultiplier(LoadItem x) => x.StartingMultiplier != 1m ? x.StartingMultiplier : x.Kind switch
    {
        LoadKind.MotorDirectOnLine => 3m, LoadKind.MotorStarDelta => 2m, LoadKind.AirConditioner => 2.5m,
        LoadKind.Welding => 1.5m, LoadKind.Elevator => 3m, _ => 1m
    };
}
