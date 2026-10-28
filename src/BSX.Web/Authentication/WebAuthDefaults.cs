namespace BSX.Web.Authentication;

/// <summary>Shared constants for the BSX web authentication surface.</summary>
public static class WebAuthDefaults
{
    /// <summary>The cookie authentication scheme used by the Blazor application.</summary>
    public const string CookieScheme = "BSX.Web.Cookie";

    /// <summary>The policy scheme that routes API requests to JWT and web requests to cookies.</summary>
    public const string SmartScheme = "BSX.Web.Smart";

    /// <summary>The authentication cookie name.</summary>
    public const string AuthCookieName = "BSX.Web.Auth";

    /// <summary>The HttpOnly refresh-token cookie name.</summary>
    public const string RefreshTokenCookieName = "BSX.Web.Refresh";

    /// <summary>The protected pending-login cookie used during tenant selection.</summary>
    public const string PendingLoginCookieName = "BSX.Web.PendingLogin";

    /// <summary>The request item used to signal that a session expired during cookie validation.</summary>
    public const string SessionExpiredItemKey = "BSX.Web.SessionExpired";

    /// <summary>The claim type carrying the user identifier.</summary>
    public const string UserIdClaim = "sub";

    /// <summary>The claim type carrying the user email.</summary>
    public const string EmailClaim = "email";

    /// <summary>The claim type carrying the display name.</summary>
    public const string NameClaim = "name";

    /// <summary>The claim type carrying the active tenant.</summary>
    public const string TenantIdClaim = "tenant_id";

    /// <summary>The claim type carrying a granted permission.</summary>
    public const string PermissionClaim = "permissions";

    /// <summary>The claim type carrying the session identifier.</summary>
    public const string SessionIdClaim = "session_id";

    /// <summary>The claim type carrying the access-token expiry as a round-trip timestamp.</summary>
    public const string AccessTokenExpiresClaim = "bsx_access_token_exp";

    /// <summary>The lifetime of the browser session before the next sliding refresh.</summary>
    public static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(7);

    /// <summary>The lifetime of a pending workspace selection.</summary>
    public static readonly TimeSpan PendingLoginLifetime = TimeSpan.FromMinutes(5);

    /// <summary>The refresh threshold before the access token is rotated.</summary>
    public static readonly TimeSpan RefreshThreshold = TimeSpan.FromMinutes(2);
}
