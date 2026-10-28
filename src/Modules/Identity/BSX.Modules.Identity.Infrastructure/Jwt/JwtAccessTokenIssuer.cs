using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BSX.Modules.Identity.Application.Abstractions;

namespace BSX.Modules.Identity.Infrastructure.Jwt;

/// <summary>Issues RS256 stateless access tokens (ADR-017).</summary>
internal sealed class JwtAccessTokenIssuer : IAccessTokenIssuer
{
    /// <summary>Claim type for the active tenant.</summary>
    public const string TenantIdClaim = "tenant_id";

    /// <summary>Claim type for the principal identifier.</summary>
    public const string PrincipalIdClaim = "principal_id";

    /// <summary>Claim type for the principal kind.</summary>
    public const string PrincipalTypeClaim = "principal_type";

    /// <summary>Claim type for the security stamp.</summary>
    public const string SecurityStampClaim = "security_stamp";

    /// <summary>Claim type for the session identifier.</summary>
    public const string SessionIdClaim = "session_id";

    /// <summary>Claim type for a granted permission.</summary>
    public const string PermissionClaim = "permissions";

    private readonly JwtKeyRing _keyRing;
    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;

    public JwtAccessTokenIssuer(JwtKeyRing keyRing, JwtOptions options, TimeProvider timeProvider)
    {
        _keyRing = keyRing;
        _options = options;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public AccessToken Issue(AccessTokenDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        DateTimeOffset nowUtc = _timeProvider.GetUtcNow();
        DateTimeOffset expiresAtUtc = nowUtc.AddMinutes(_options.AccessTokenLifetimeMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, descriptor.UserId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(PrincipalIdClaim, descriptor.PrincipalId.ToString()),
            new(PrincipalTypeClaim, descriptor.PrincipalType),
            new(JwtRegisteredClaimNames.Email, descriptor.Email),
            new(JwtRegisteredClaimNames.Name, descriptor.Name),
            new(SecurityStampClaim, descriptor.SecurityStamp),
            new(SessionIdClaim, descriptor.SessionId.ToString()),
        };

        if (descriptor.TenantId is { } tenantId)
        {
            claims.Add(new Claim(TenantIdClaim, tenantId.ToString()));
        }

        foreach (string permission in descriptor.Permissions)
        {
            claims.Add(new Claim(PermissionClaim, permission));
        }

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: nowUtc.UtcDateTime,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: _keyRing.GetSigningCredentials());

        string encoded = new JwtSecurityTokenHandler().WriteToken(token);
        return new AccessToken(encoded, expiresAtUtc);
    }
}
