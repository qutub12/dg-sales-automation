using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace DgSales.Api.Application;

public sealed class QuotationDocumentTokenService(IConfiguration configuration, TimeProvider timeProvider)
{
    public (long ExpiresUnix, string Signature) Create(Guid quotationId, TimeSpan lifetime)
    {
        var expires = timeProvider.GetUtcNow().Add(lifetime).ToUnixTimeSeconds();
        return (expires, Sign(quotationId, expires));
    }

    public bool Validate(Guid quotationId, long expiresUnix, string signature)
    {
        if (expiresUnix < timeProvider.GetUtcNow().ToUnixTimeSeconds() || string.IsNullOrWhiteSpace(signature)) return false;
        var expected = Sign(quotationId, expiresUnix);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(signature));
    }

    private string Sign(Guid quotationId, long expiresUnix)
    {
        var secret = configuration["QuotationDocuments:SigningSecret"];
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
            throw new InvalidOperationException("QuotationDocuments:SigningSecret must contain at least 32 characters.");
        var payload = $"{quotationId:N}:{expiresUnix.ToString(CultureInfo.InvariantCulture)}";
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
