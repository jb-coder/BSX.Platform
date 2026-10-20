using System.Security.Cryptography;
using BSX.Modules.Identity.Infrastructure.Jwt;

namespace BSX.Modules.Identity.Infrastructure.IntegrationTests;

/// <summary>Hardening rules for JWT options (D1): ephemeral keys only in Development, fail fast otherwise.</summary>
public sealed class JwtOptionsConfigurationTests
{
    [Fact]
    public void Should_GenerateEphemeralKey_WhenDevelopmentAndNoKeys()
    {
        var options = new JwtOptions { Issuer = "dev", Audience = "dev" };

        JwtOptionsConfiguration.Apply(options, isDevelopment: true);

        options.SigningKeys.Should().ContainSingle();
        options.SigningKeys[0].Kid.Should().Be("development");
        options.SigningKeys[0].PrivateKeyPem.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Should_FailFast_WhenProductionAndNoKeys()
    {
        var options = new JwtOptions { Issuer = "prod", Audience = "prod" };

        Action act = () => JwtOptionsConfiguration.Apply(options, isDevelopment: false);

        act.Should().Throw<InvalidOperationException>().WithMessage("*SigningKeys*");
    }

    [Fact]
    public void Should_PreserveConfiguredKeys()
    {
        using var rsa = RSA.Create(2048);
        var options = new JwtOptions { Issuer = "prod", Audience = "prod" };
        options.SigningKeys.Add(new JwtSigningKey { Kid = "k1", PrivateKeyPem = rsa.ExportRSAPrivateKeyPem() });

        JwtOptionsConfiguration.Apply(options, isDevelopment: false);

        options.SigningKeys.Should().ContainSingle();
        options.SigningKeys[0].Kid.Should().Be("k1");
    }

    [Theory]
    [InlineData("", "aud")]
    [InlineData("iss", "")]
    [InlineData("  ", "aud")]
    public void Should_Reject_InvalidMetadata(string issuer, string audience)
    {
        var options = new JwtOptions { Issuer = issuer, Audience = audience };

        Action act = () => JwtOptionsConfiguration.Apply(options, isDevelopment: true);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Should_Reject_NonPositiveLifetime()
    {
        var options = new JwtOptions { Issuer = "iss", Audience = "aud", AccessTokenLifetimeMinutes = 0 };

        Action act = () => JwtOptionsConfiguration.Apply(options, isDevelopment: true);

        act.Should().Throw<InvalidOperationException>().WithMessage("*AccessTokenLifetimeMinutes*");
    }
}
