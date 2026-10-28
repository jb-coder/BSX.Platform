namespace BSX.Web.Authentication;

/// <summary>Writes and removes the HttpOnly refresh-token cookie.</summary>
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

        httpContext.Response.Cookies.Append(
            WebAuthDefaults.RefreshTokenCookieName,
            refreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = httpContext.Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                Expires = expiresAtUtc,
                IsEssential = true,
            });
    }

    /// <summary>Deletes the refresh-token cookie.</summary>
    /// <param name="httpContext">The current HTTP context.</param>
    public static void DeleteRefreshToken(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        httpContext.Response.Cookies.Delete(
            WebAuthDefaults.RefreshTokenCookieName,
            new CookieOptions { Path = "/" });
    }
}
