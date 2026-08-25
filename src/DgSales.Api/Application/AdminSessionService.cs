using System.Security.Cryptography;
using System.Text;

namespace DgSales.Api.Application;

public sealed class AdminSessionService(IConfiguration config, TimeProvider clock)
{
    public const string CookieName = "dg_admin";

    public bool ValidateCredentials(string username, string password)
    {
        if (!Fixed(username, Required("Admin:Username"))) return false;
        var hash = config["Admin:PasswordHash"];
        if (!string.IsNullOrWhiteSpace(hash)) return AdminPasswordHasher.Verify(password, hash);
        return !string.IsNullOrWhiteSpace(config["Admin:Password"]) && Fixed(password, config["Admin:Password"]!);
    }

    public string CreateToken()
    {
        var expires = clock.GetUtcNow().AddHours(8).ToUnixTimeSeconds();
        var payload = $"{Required("Admin:Username")}|{expires}";
        return $"{Convert.ToBase64String(Encoding.UTF8.GetBytes(payload))}.{Sign(payload)}";
    }

    public bool ValidateToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;
        var parts = token.Split('.', 2); if (parts.Length != 2) return false;
        string payload;
        try { payload = Encoding.UTF8.GetString(Convert.FromBase64String(parts[0])); } catch (FormatException) { return false; }
        var values = payload.Split('|', 2);
        return values.Length == 2 && values[0] == Required("Admin:Username")
            && long.TryParse(values[1], out var expiry) && expiry >= clock.GetUtcNow().ToUnixTimeSeconds()
            && Fixed(parts[1], Sign(payload));
    }

    private string Sign(string value) => Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(Required("Admin:CookieSigningKey")), Encoding.UTF8.GetBytes(value)));
    private string Required(string key) => config[key] is { Length: > 0 } value ? value : throw new InvalidOperationException($"{key} is required.");
    private static bool Fixed(string left, string right)
    {
        var a = Encoding.UTF8.GetBytes(left); var b = Encoding.UTF8.GetBytes(right);
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }
}

public sealed record AdminLoginRequest(string Username, string Password);
