using System.Globalization;
using System.Security.Claims;
using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Application.Authentication;

namespace BSX.Web.Authentication;

/// <summary>Builds the cookie principal from a validated Identity access token or a revalidated snapshot.</summary>
public static class WebPrincipalFactory
{
    /// <summary>Creates a claims principal for the web session.</summary>
    /// <param name="token">The validated access token.</param>
    public static ClaimsPrincipal Create(ValidatedAccessToken token)
    {
        ArgumentNullException.ThrowIfNull(token);

        return CreateCore(
            token.UserId,
            token.TenantId,
            token.SessionId,
            token.Email,
            token.Name,
            token.Permissions,
            token.ExpiresAtUtc);
    }

    /// <summary>
    /// Creates a claims principal from a freshly revalidated snapshot while preserving the original
    /// access-token expiry so the cookie refresh flow keeps working (ADR-021).
    /// </summary>
    /// <param name="snapshot">The authoritative authorization snapshot.</param>
    /// <param name="accessTokenExpiresAtUtc">The access-token expiry carried by the current principal.</param>
    public static ClaimsPrincipal Create(AuthorizationSnapshot snapshot, DateTimeOffset accessTokenExpiresAtUtc)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return CreateCore(
            snapshot.UserId,
            snapshot.TenantId,
            snapshot.SessionId,
            snapshot.Email,
            snapshot.Name,
            snapshot.Permissions,
            accessTokenExpiresAtUtc);
    }

    /// <summary>Reads the access-token expiry carried by a principal, if present.</summary>
    /// <param name="principal">The principal.</param>
    public static DateTimeOffset? GetAccessTokenExpiry(ClaimsPrincipal? principal)
    {
        string? value = principal?.FindFirst(WebAuthDefaults.AccessTokenExpiresClaim)?.Value;

        return DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out DateTimeOffset expiresAt)
            ? expiresAt
            : null;
    }

    private static ClaimsPrincipal CreateCore(
        Guid userId,
        Guid? tenantId,
        Guid sessionId,
        string email,
        string name,
        IReadOnlyCollection<string> permissions,
        DateTimeOffset accessTokenExpiresAtUtc)
    {
        var claims = new List<Claim>
        {
            new(WebAuthDefaults.UserIdClaim, userId.ToString()),
            new(WebAuthDefaults.EmailClaim, email),
            new(WebAuthDefaults.NameClaim, name),
            new(WebAuthDefaults.SessionIdClaim, sessionId.ToString()),
            new(WebAuthDefaults.AccessTokenExpiresClaim, accessTokenExpiresAtUtc.ToString("O")),
        };

        if (tenantId is { } activeTenantId)
        {
            claims.Add(new Claim(WebAuthDefaults.TenantIdClaim, activeTenantId.ToString()));
        }

        foreach (string permission in permissions)
        {
            claims.Add(new Claim(WebAuthDefaults.PermissionClaim, permission));
        }

        var identity = new ClaimsIdentity(
            claims,
            authenticationType: WebAuthDefaults.CookieScheme,
            nameType: WebAuthDefaults.NameClaim,
            roleType: ClaimTypes.Role);

        return new ClaimsPrincipal(identity);
    }
}
