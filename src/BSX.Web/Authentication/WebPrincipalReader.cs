using System.Security.Claims;

namespace BSX.Web.Authentication;

/// <summary>Reads the identity, session and tenant identifiers from a web principal.</summary>
internal static class WebPrincipalReader
{
    /// <summary>
    /// Reads the principal identifiers. Returns <see langword="false"/> when the principal is not
    /// authenticated or does not carry the mandatory user and session identifiers.
    /// </summary>
    /// <param name="principal">The principal.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="tenantId">The active tenant, if any.</param>
    public static bool TryRead(
        ClaimsPrincipal? principal,
        out Guid userId,
        out Guid sessionId,
        out Guid? tenantId)
    {
        userId = Guid.Empty;
        sessionId = Guid.Empty;
        tenantId = null;

        if (principal?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (!Guid.TryParse(principal.FindFirst(WebAuthDefaults.UserIdClaim)?.Value, out userId)
            || !Guid.TryParse(principal.FindFirst(WebAuthDefaults.SessionIdClaim)?.Value, out sessionId))
        {
            return false;
        }

        if (Guid.TryParse(principal.FindFirst(WebAuthDefaults.TenantIdClaim)?.Value, out Guid tenant))
        {
            tenantId = tenant;
        }

        return true;
    }
}
