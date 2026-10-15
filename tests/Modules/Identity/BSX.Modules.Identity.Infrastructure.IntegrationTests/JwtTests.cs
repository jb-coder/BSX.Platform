using System.Security.Cryptography;
using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Infrastructure.Jwt;

namespace BSX.Modules.Identity.Infrastructure.IntegrationTests;

public sealed class JwtTests
{
    [Fact]
    public void Should_IssueAndValidate_Token()
    {
        JwtOptions options = CreateOptions();
        using var keyRing = new JwtKeyRing(options);
        var issuer = new JwtAccessTokenIssuer(keyRing, options, TimeProvider.System);
        var validator = new JwtAccessTokenValidator(keyRing, options);

        var descriptor = new AccessTokenDescriptor(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "User",
            ["crm.customers.read", "crm.customers.create"],
            "stamp-value",
            Guid.NewGuid());

        AccessToken token = issuer.Issue(descriptor);

        validator.TryValidate(token.Token, out ValidatedAccessToken principal).Should().BeTrue();
        principal.UserId.Should().Be(descriptor.UserId);
        principal.TenantId.Should().Be(descriptor.TenantId);
        principal.PrincipalId.Should().Be(descriptor.PrincipalId);
        principal.PrincipalType.Should().Be("User");
        principal.Permissions.Should().BeEquivalentTo(descriptor.Permissions);
        principal.SecurityStamp.Should().Be("stamp-value");
        principal.SessionId.Should().Be(descriptor.SessionId);
    }

    [Fact]
    public void Should_Reject_TamperedToken()
    {
        JwtOptions options = CreateOptions();
        using var keyRing = new JwtKeyRing(options);
        var issuer = new JwtAccessTokenIssuer(keyRing, options, TimeProvider.System);
        var validator = new JwtAccessTokenValidator(keyRing, options);

        AccessToken token = issuer.Issue(new AccessTokenDescriptor(
            Guid.NewGuid(), null, Guid.NewGuid(), "User", [], "stamp", Guid.NewGuid()));

        validator.TryValidate(token.Token + "x", out _).Should().BeFalse();
    }

    [Fact]
    public void Should_Expose_JwksReadyValidationKeys()
    {
        JwtOptions options = CreateOptions();
        using var keyRing = new JwtKeyRing(options);

        keyRing.GetValidationKeys().Should().ContainSingle();
    }

    private static JwtOptions CreateOptions()
    {
        using var rsa = RSA.Create(2048);
        string privateKeyPem = rsa.ExportRSAPrivateKeyPem();

        var options = new JwtOptions { Issuer = "test-issuer", Audience = "test-audience", AccessTokenLifetimeMinutes = 5 };
        options.SigningKeys.Add(new JwtSigningKey { Kid = "k1", PrivateKeyPem = privateKeyPem });
        return options;
    }
}
