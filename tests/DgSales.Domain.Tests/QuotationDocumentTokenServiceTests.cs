using DgSales.Api.Application;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DgSales.Domain.Tests;

public sealed class QuotationDocumentTokenServiceTests
{
    [Fact]
    public void AcceptsValidTokenAndRejectsTampering()
    {
        var now = new DateTimeOffset(2026, 8, 19, 0, 0, 0, TimeSpan.Zero);
        var service = Create(now);
        var quotationId = Guid.NewGuid();
        var token = service.Create(quotationId, TimeSpan.FromMinutes(10));

        Assert.True(service.Validate(quotationId, token.ExpiresUnix, token.Signature));
        Assert.False(service.Validate(Guid.NewGuid(), token.ExpiresUnix, token.Signature));
    }

    [Fact]
    public void RejectsExpiredToken()
    {
        var service = Create(new DateTimeOffset(2026, 8, 19, 0, 0, 0, TimeSpan.Zero));
        var quotationId = Guid.NewGuid();
        var token = service.Create(quotationId, TimeSpan.FromMinutes(-1));

        Assert.False(service.Validate(quotationId, token.ExpiresUnix, token.Signature));
    }

    private static QuotationDocumentTokenService Create(DateTimeOffset now)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["QuotationDocuments:SigningSecret"] = "test-secret-with-at-least-thirty-two-characters"
        }).Build();
        return new(configuration, new FixedTimeProvider(now));
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
