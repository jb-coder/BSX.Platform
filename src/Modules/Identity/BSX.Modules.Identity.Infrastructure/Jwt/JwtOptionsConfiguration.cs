using System.Security.Cryptography;

namespace BSX.Modules.Identity.Infrastructure.Jwt;

/// <summary>
/// Validates and completes the JWT options at startup (ADR-017 hardening).
/// Signing keys are mandatory outside Development; an ephemeral key is generated only in
/// Development so that a misconfigured production host fails fast instead of silently signing
/// tokens with a key that is lost on restart and cannot be shared across instances.
/// </summary>
public static class JwtOptionsConfiguration
{
    /// <summary>Applies hardening rules to the bound <see cref="JwtOptions"/>.</summary>
    /// <param name="options">The bound options.</param>
    /// <param name="isDevelopment">Whether the host runs in the Development environment.</param>
    /// <exception cref="InvalidOperationException">When the configuration is invalid or keys are missing outside Development.</exception>
    public static void Apply(JwtOptions options, bool isDevelopment)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            throw new InvalidOperationException("Identity:Jwt:Issuer must be configured.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            throw new InvalidOperationException("Identity:Jwt:Audience must be configured.");
        }

        if (options.AccessTokenLifetimeMinutes <= 0)
        {
            throw new InvalidOperationException("Identity:Jwt:AccessTokenLifetimeMinutes must be greater than zero.");
        }

        if (options.SigningKeys.Any(key => string.IsNullOrWhiteSpace(key.PrivateKeyPem)))
        {
            throw new InvalidOperationException("Every Identity:Jwt:SigningKeys entry must define a PrivateKeyPem.");
        }

        if (options.SigningKeys.Count > 0)
        {
            return;
        }

        if (!isDevelopment)
        {
            throw new InvalidOperationException(
                "Identity:Jwt:SigningKeys must be configured outside the Development environment. "
                + "Refusing to start with an ephemeral signing key.");
        }

        using RSA rsa = RSA.Create(2048);
        options.SigningKeys.Add(new JwtSigningKey
        {
            Kid = "development",
            PrivateKeyPem = rsa.ExportRSAPrivateKeyPem(),
        });
    }
}
