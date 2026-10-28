using System.Security.Claims;
using BSX.Modules.Identity.Application.Abstractions;

namespace BSX.Web.Authentication;

/// <summary>Builds the cookie principal from a validated Identity access token.</summary>
public static class WebPrincipalFactory
{
    /// <summary>Creates a claims principal for the web session.</summary>
    /// <param name="token">The validated access token.</param>
    public static ClaimsPrincipal Create(ValidatedAccessToken token)
    {
        ArgumentNullException.ThrowIfNull(token);

        var claims = new List<Claim>
        {
            new(WebAuthDefaults.UserIdClaim, token.UserId.ToString()),
            new(WebAuthDefaults.EmailClaim, token.Email),
            new(WebAuthDefaults.NameClaim, token.Name),
            new(WebAuthDefaults.SessionIdClaim, token.SessionId.ToString()),
            new(WebAuthDefaults.AccessTokenExpiresClaim, token.ExpiresAtUtc.ToString("O")),
        };

        if (token.TenantId is { } tenantId)
        {
            claims.Add(new Claim(WebAuthDefaults.TenantIdClaim, tenantId.ToString()));
        }

        foreach (string permission in token.Permissions)
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
