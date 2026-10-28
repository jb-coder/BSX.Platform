using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BSX.Modules.Identity.Application.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace BSX.Modules.Identity.Infrastructure.Jwt;

/// <summary>Validates RS256 access tokens issued by <see cref="JwtAccessTokenIssuer"/> (ADR-017).</summary>
internal sealed class JwtAccessTokenValidator : IAccessTokenValidator
{
    private readonly TokenValidationParameters _parameters;

    public JwtAccessTokenValidator(JwtKeyRing keyRing, JwtOptions options)
    {
        _parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = keyRing.GetValidationKeys(),
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
        };
    }

    /// <inheritdoc />
    public bool TryValidate(string token, out ValidatedAccessToken principal)
    {
        principal = null!;

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };

        try
        {
            ClaimsPrincipal validated = handler.ValidateToken(token, _parameters, out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwt
                || !Guid.TryParse(validated.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out Guid userId)
                || !Guid.TryParse(validated.FindFirst(JwtAccessTokenIssuer.PrincipalIdClaim)?.Value, out Guid principalId)
                || !Guid.TryParse(validated.FindFirst(JwtAccessTokenIssuer.SessionIdClaim)?.Value, out Guid sessionId))
            {
                return false;
            }

            Guid? tenantId = Guid.TryParse(validated.FindFirst(JwtAccessTokenIssuer.TenantIdClaim)?.Value, out Guid tenant)
                ? tenant
                : null;

            IReadOnlyCollection<string> permissions = validated
                .FindAll(JwtAccessTokenIssuer.PermissionClaim)
                .Select(claim => claim.Value)
                .ToArray();

            principal = new ValidatedAccessToken(
                userId,
                tenantId,
                principalId,
                validated.FindFirst(JwtAccessTokenIssuer.PrincipalTypeClaim)?.Value ?? string.Empty,
                validated.FindFirst(JwtRegisteredClaimNames.Email)?.Value ?? string.Empty,
                validated.FindFirst(JwtRegisteredClaimNames.Name)?.Value ?? string.Empty,
                permissions,
                validated.FindFirst(JwtAccessTokenIssuer.SecurityStampClaim)?.Value ?? string.Empty,
                sessionId,
                new DateTimeOffset(DateTime.SpecifyKind(jwt.ValidTo, DateTimeKind.Utc)));

            return true;
        }
        catch (SecurityTokenException)
        {
            return false;
        }
    }
}
