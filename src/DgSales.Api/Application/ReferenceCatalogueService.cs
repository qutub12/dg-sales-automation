using System.Text.Json;

namespace DgSales.Api.Application;

public sealed record TechnicalCatalogueReference(
    string Brand, string GensetModel, decimal Kva, decimal Kwe, int PhaseCount,
    string EngineModel, string Compliance, string SourceDocument, bool RequiresTechnicalReview = false);

public sealed class ReferenceCatalogueService(IHostEnvironment environment)
{
    private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

    public IReadOnlyCollection<TechnicalCatalogueReference> GetTechnical() =>
        Read<TechnicalCatalogueReference>("technical-catalogue-reference.json");

    private IReadOnlyCollection<T> Read<T>(string fileName)
    {
        var path = Path.Combine(environment.ContentRootPath, "data", fileName);
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<T[]>(stream, _options)
            ?? throw new InvalidOperationException($"Reference file {fileName} is empty.");
    }
}
