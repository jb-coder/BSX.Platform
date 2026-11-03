using BSX.Web.Deployment;

namespace BSX.Web.Authentication;

/// <summary>Writes and removes the HttpOnly authentication, refresh and pending-selection cookies.</summary>
public static class WebAuthCookieWriter
{
    /// <summary>Stores the refresh token in an HttpOnly cookie.</summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="refreshToken">The refresh token.</param>
    /// <param name="expiresAtUtc">The cookie expiry.</param>
    public static void AppendRefreshToken(
        HttpContext httpContext,
        string refreshToken,
        DateTimeOffset expiresAtUtc)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        WebCookieOptions options = ResolveOptions(httpContext);

        httpContext.Response.Cookies.Append(
            WebAuthDefaults.RefreshTokenCookieName,
            refreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = IsSecure(httpContext, options),
                SameSite = options.RefreshSameSite,
                Domain = options.Domain,
                Path = "/",
                Expires = expiresAtUtc,
                IsEssential = true,
            });
    }

    /// <summary>Stores the pending workspace-selection secret in an HttpOnly cookie.</summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="pendingLoginToken">The secret pending-login token.</param>
    /// <param name="expiresAtUtc">The cookie expiry.</param>
    public static void AppendPendingLogin(
        HttpContext httpContext,
        string pendingLoginToken,
        DateTimeOffset expiresAtUtc)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(pendingLoginToken);

        WebCookieOptions options = ResolveOptions(httpContext);

        httpContext.Response.Cookies.Append(
            WebAuthDefaults.PendingLoginCookieName,
            pendingLoginToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = IsSecure(httpContext, options),
                SameSite = options.PendingLoginSameSite,
                Domain = options.Domain,
                Path = "/",
                Expires = expiresAtUtc,
                IsEssential = true,
            });
    }

    /// <summary>Deletes the pending workspace-selection cookie.</summary>
    /// <param name="httpContext">The current HTTP context.</param>
    public static void DeletePendingLogin(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        WebCookieOptions options = ResolveOptions(httpContext);

        httpContext.Response.Cookies.Delete(
            WebAuthDefaults.PendingLoginCookieName,
            new CookieOptions
            {
                Path = "/",
                Domain = options.Domain,
                Secure = IsSecure(httpContext, options),
            });
    }

    /// <summary>Deletes the refresh-token cookie.</summary>
    /// <param name="httpContext">The current HTTP context.</param>
    public static void DeleteRefreshToken(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        WebCookieOptions options = ResolveOptions(httpContext);

        httpContext.Response.Cookies.Delete(
            WebAuthDefaults.RefreshTokenCookieName,
            new CookieOptions
            {
                Path = "/",
                Domain = options.Domain,
                Secure = IsSecure(httpContext, options),
            });
    }

    internal static WebCookieOptions ResolveOptions(HttpContext httpContext)
        => httpContext.RequestServices.GetRequiredService<WebCookieOptions>();

    internal static bool IsSecure(HttpContext httpContext, WebCookieOptions options)
        => options.SecurePolicy switch
        {
            CookieSecurePolicy.Always => true,
            CookieSecurePolicy.SameAsRequest => httpContext.Request.IsHttps,
            _ => false,
        };
}
