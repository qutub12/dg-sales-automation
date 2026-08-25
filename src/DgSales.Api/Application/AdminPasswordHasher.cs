using System.Security.Cryptography;

namespace DgSales.Api.Application;

public static class AdminPasswordHasher
{
    private const int SaltBytes = 16;
    private const int HashBytes = 32;
    private const int MinimumIterations = 100_000;

    public static string Hash(string password, int iterations = 210_000)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
            throw new ArgumentException("Admin password must contain at least 12 characters.");
        if (iterations < MinimumIterations) throw new ArgumentOutOfRangeException(nameof(iterations));
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, HashBytes);
        return $"pbkdf2-sha256${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string encoded)
    {
        var parts = encoded.Split('$');
        if (parts.Length != 4 || parts[0] != "pbkdf2-sha256" || !int.TryParse(parts[1], out var iterations)
            || iterations < MinimumIterations) return false;
        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException) { return false; }
    }
}
