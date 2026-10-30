using System.Globalization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Application.Authentication;
using BSX.Modules.Identity.Application.Authentication.RefreshToken;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace BSX.Web.Authentication;

/// <summary>
/// Validates the web authentication cookie, revalidates the session against the Identity store and
/// transparently rotates the Identity access token before it expires. Failures fail closed and send
/// the user to the sign-in experience (ADR-021).
/// </summary>
public sealed class WebCookieAuthenticationEvents : CookieAuthenticationEvents
{
    private readonly ISender _sender;
    private readonly IAccessTokenValidator _tokenValidator;
    private readonly IAuthorizationSnapshotReader _snapshotReader;
    private readonly AuthorizationRevalidationOptions _revalidationOptions;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<WebCookieAuthenticationEvents> _logger;

    /// <summary>Initializes a new instance of the <see cref="WebCookieAuthenticationEvents"/> class.</summary>
    public WebCookieAuthenticationEvents(
        ISender sender,
        IAccessTokenValidator tokenValidator,
        IAuthorizationSnapshotReader snapshotReader,
        IOptions<AuthorizationRevalidationOptions> revalidationOptions,
        TimeProvider timeProvider,
        ILogger<WebCookieAuthenticationEvents> logger)
    {
        _sender = sender;
        _tokenValidator = tokenValidator;
        _snapshotReader = snapshotReader;
        _revalidationOptions = revalidationOptions.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        DateTimeOffset nowUtc = _timeProvider.GetUtcNow();

        if (ShouldRevalidate(context, nowUtc) && !await RevalidateAsync(context, nowUtc).ConfigureAwait(false))
        {
            return;
        }

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

        if (expiresAt > nowUtc.Add(WebAuthDefaults.RefreshThreshold))
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
            context.Properties.ExpiresUtc = nowUtc.Add(WebAuthDefaults.SessionLifetime);
            context.ShouldRenew = true;
            context.Properties.Items[WebAuthDefaults.LastRevalidatedAtItemKey] = nowUtc.ToString("O", CultureInfo.InvariantCulture);

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

    private bool ShouldRevalidate(CookieValidatePrincipalContext context, DateTimeOffset nowUtc)
    {
        if (context.Principal?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (!context.Properties.Items.TryGetValue(WebAuthDefaults.LastRevalidatedAtItemKey, out string? value)
            || !DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out DateTimeOffset lastRevalidatedAtUtc))
        {
            return true;
        }

        return nowUtc - lastRevalidatedAtUtc >= _revalidationOptions.CookieInterval;
    }

    private async Task<bool> RevalidateAsync(CookieValidatePrincipalContext context, DateTimeOffset nowUtc)
    {
        if (!WebPrincipalReader.TryRead(context.Principal, out Guid userId, out Guid sessionId, out Guid? tenantId))
        {
            ExpireSession(context, "The web session had no user or session identifier.");
            return false;
        }

        try
        {
            AuthorizationSnapshot? snapshot = await _snapshotReader
                .ReadAsync(userId, sessionId, tenantId, context.HttpContext.RequestAborted)
                .ConfigureAwait(false);

            if (snapshot is null)
            {
                ExpireSession(context, "The web session is no longer valid.");
                return false;
            }

            DateTimeOffset accessTokenExpiresAtUtc = WebPrincipalFactory.GetAccessTokenExpiry(context.Principal)
                ?? nowUtc.Add(WebAuthDefaults.SessionLifetime);

            context.ReplacePrincipal(WebPrincipalFactory.Create(snapshot, accessTokenExpiresAtUtc));
            context.Properties.Items[WebAuthDefaults.LastRevalidatedAtItemKey] = nowUtc.ToString("O", CultureInfo.InvariantCulture);
            context.ShouldRenew = true;
            return true;
        }
        catch (OperationCanceledException) when (context.HttpContext.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Revalidating the web session failed; the user will sign in again.");
            ExpireSession(context, "The web session could not be revalidated.");
            return false;
        }
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
