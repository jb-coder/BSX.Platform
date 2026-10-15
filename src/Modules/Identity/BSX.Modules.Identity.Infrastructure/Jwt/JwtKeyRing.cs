using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace BSX.Modules.Identity.Infrastructure.Jwt;

/// <summary>Holds the asymmetric signing keys and exposes signing/validation material (ADR-017).</summary>
internal sealed class JwtKeyRing : IDisposable
{
    private readonly List<(string Kid, RSA Rsa)> _keys = [];

    public JwtKeyRing(JwtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        foreach (JwtSigningKey key in options.SigningKeys)
        {
            RSA rsa = RSA.Create();
            rsa.ImportFromPem(key.PrivateKeyPem);
            _keys.Add((key.Kid, rsa));
        }

        if (_keys.Count == 0)
        {
            throw new InvalidOperationException("At least one JWT signing key is required.");
        }
    }

    /// <summary>Gets the active signing credentials.</summary>
    public SigningCredentials GetSigningCredentials()
    {
        (string kid, RSA rsa) = _keys[0];
        return new SigningCredentials(new RsaSecurityKey(rsa) { KeyId = kid }, SecurityAlgorithms.RsaSha256);
    }

    /// <summary>Gets the public validation keys (JWKS-ready).</summary>
    public IReadOnlyCollection<SecurityKey> GetValidationKeys()
        => _keys.Select(key => (SecurityKey)new RsaSecurityKey(key.Rsa) { KeyId = key.Kid }).ToList();

    /// <inheritdoc />
    public void Dispose()
    {
        foreach ((_, RSA rsa) in _keys)
        {
            rsa.Dispose();
        }

        _keys.Clear();
    }
}
