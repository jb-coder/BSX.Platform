namespace BSX.Modules.Identity.Infrastructure.Jwt;

/// <summary>A signing key entry (kid + RSA private key PEM).</summary>
public sealed class JwtSigningKey
{
    /// <summary>Gets or sets the key identifier (kid).</summary>
    public string Kid { get; set; } = string.Empty;

    /// <summary>Gets or sets the RSA private key in PEM (PKCS#8) format.</summary>
    public string PrivateKeyPem { get; set; } = string.Empty;
}

/// <summary>JWT issuance/validation options (ADR-017).</summary>
public sealed class JwtOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Identity:Jwt";

    /// <summary>Gets or sets the token issuer.</summary>
    public string Issuer { get; set; } = "bsx-platform";

    /// <summary>Gets or sets the token audience.</summary>
    public string Audience { get; set; } = "bsx-platform";

    /// <summary>Gets or sets the access-token lifetime in minutes (short-lived).</summary>
    public int AccessTokenLifetimeMinutes { get; set; } = 15;

    /// <summary>Gets the signing keys; the first is the active signing key.</summary>
    public IList<JwtSigningKey> SigningKeys { get; } = [];
}
