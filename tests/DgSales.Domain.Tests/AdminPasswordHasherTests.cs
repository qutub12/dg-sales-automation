using DgSales.Api.Application;
using Xunit;

namespace DgSales.Domain.Tests;

public sealed class AdminPasswordHasherTests
{
    [Fact]
    public void HashesAndVerifiesWithoutStoringPlainPassword()
    {
        const string password = "A-strong-owner-password-2026";
        var encoded = AdminPasswordHasher.Hash(password, 100_000);
        Assert.StartsWith("pbkdf2-sha256$100000$", encoded);
        Assert.DoesNotContain(password, encoded);
        Assert.True(AdminPasswordHasher.Verify(password, encoded));
        Assert.False(AdminPasswordHasher.Verify("wrong-password", encoded));
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    public void RejectsWeakPasswords(string password) =>
        Assert.Throws<ArgumentException>(() => AdminPasswordHasher.Hash(password));
}
