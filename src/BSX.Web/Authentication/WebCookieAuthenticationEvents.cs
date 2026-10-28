using System.Globalization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Application.Authentication.RefreshToken;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.WebUtilities;

namespace BSX.Web.Authentication;

/// <summary>
/// Validates the web authentication cookie and transparently rotates the Identity access token
/// before it expires. Refresh failures fail closed and send the user to the sign-in experience.
/// </summary>
public sealed class WebCookieAuthenticationEvents : CookieAuthenticationEvents
{
    private readonly ISender _sender;
    private readonly IAccessTokenValidator _tokenValidator;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<WebCookieAuthenticationEvents> _logger;

    /// <summary>Initializes a new instance of the <see cref="WebCookieAuthenticationEvents"/> class.</summary>
    public WebCookieAuthenticationEvents(
        ISender sender,
        IAccessTokenValidator tokenValidator,
        TimeProvider timeProvider,
        ILogger<WebCookieAuthenticationEvents> logger)
    {
        _sender = sender;
        _tokenValidator = tokenValidator;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string? expiryValue = context.Principal?.FindFirst(WebAuthDefaults.AccessTokenExpiresClaim)?.Value;
        if (!DateTimeOffset.TryParse(
            expiryValue,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out DateTimeOffset expiresAt))
        {
            ExpireSession(context, "The web session had no access-token expiry.");
            return;
        }

        if (expiresAt > _timeProvider.GetUtcNow().Add(WebAuthDefaults.RefreshThreshold))
        {
            return;
        }

        string? refreshToken = context.HttpContext.Request.Cookies[WebAuthDefaults.RefreshTokenCookieName];
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            ExpireSession(context, "The web session had no refresh token.");
            return;
        }

        try
        {
            var refreshed = await _sender
                .SendAsync(new RefreshTokenCommand(refreshToken), context.HttpContext.RequestAborted)
                .ConfigureAwait(false);

            if (refreshed.IsFailure
                || !_tokenValidator.TryValidate(refreshed.Value.AccessToken, out ValidatedAccessToken token))
            {
                ExpireSession(context, "The web session could not be refreshed.");
                return;
            }

            context.ReplacePrincipal(WebPrincipalFactory.Create(token));
            context.Properties.ExpiresUtc = _timeProvider.GetUtcNow().Add(WebAuthDefaults.SessionLifetime);
            context.ShouldRenew = true;

            WebAuthCookieWriter.AppendRefreshToken(
                context.HttpContext,
                refreshed.Value.RefreshToken,
                context.Properties.ExpiresUtc.Value);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Refreshing the web session failed; the user will sign in again.");
            ExpireSession(context, "The web session refresh failed.");
        }
    }

    /// <inheritdoc />
    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string redirect = context.RedirectUri;

        if (context.HttpContext.Items.ContainsKey(WebAuthDefaults.SessionExpiredItemKey))
        {
            redirect = QueryHelpers.AddQueryString(redirect, "expired", "true");
        }

        context.Response.Redirect(redirect);
        return Task.CompletedTask;
    }

    private static void ExpireSession(CookieValidatePrincipalContext context, string reason)
    {
        context.RejectPrincipal();
        context.HttpContext.Items[WebAuthDefaults.SessionExpiredItemKey] = reason;
        context.HttpContext.Response.Cookies.Delete(
            WebAuthDefaults.AuthCookieName,
            new CookieOptions { Path = "/" });
        WebAuthCookieWriter.DeleteRefreshToken(context.HttpContext);
    }
}
