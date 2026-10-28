using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Application.Authentication.DiscoverTenantOptions;
using BSX.Modules.Identity.Application.Authentication.Login;
using BSX.Modules.Identity.Application.Authentication.Logout;
using BSX.Modules.Identity.Application.Dtos;
using BSX.SharedKernel.Results;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.WebUtilities;

namespace BSX.Web.Authentication;

/// <summary>
/// Thin web authentication endpoints over the Identity application commands. The endpoints set the
/// web cookie and never expose Identity tokens to the browser.
/// </summary>
public static class WebAuthEndpoints
{
    /// <summary>Maps the web sign-in, workspace selection and sign-out endpoints.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The same builder for chaining.</returns>
    public static IEndpointRouteBuilder MapWebAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost("/auth/login", (Delegate)LoginAsync).AllowAnonymous();
        endpoints.MapPost("/auth/select-tenant", (Delegate)SelectTenantAsync).AllowAnonymous();
        endpoints.MapPost("/auth/logout", (Delegate)LogoutAsync).AllowAnonymous();

        return endpoints;
    }

    private static async Task<IResult> LoginAsync(HttpContext httpContext)
    {
        if (!await ValidateAntiforgeryAsync(httpContext).ConfigureAwait(false))
        {
            return RedirectToLoginError("invalid_request");
        }

        IFormCollection form = await httpContext.Request.ReadFormAsync(httpContext.RequestAborted).ConfigureAwait(false);
        string email = form["email"].ToString().Trim();
        string password = form["password"].ToString();
        string? returnUrl = form["returnUrl"].ToString();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return RedirectToLoginError("invalid_credentials");
        }

        ISender sender = httpContext.RequestServices.GetRequiredService<ISender>();
        PendingLoginStore pendingLogins = httpContext.RequestServices.GetRequiredService<PendingLoginStore>();

        var options = await sender
            .SendAsync(new DiscoverTenantOptionsCommand(email, password), httpContext.RequestAborted)
            .ConfigureAwait(false);

        if (options.IsFailure)
        {
            return RedirectToLoginError(MapError(options.Error));
        }

        if (options.Value.Count > 1)
        {
            string token = pendingLogins.Save(new PendingLogin(
                email,
                password,
                SafeReturnUrl(returnUrl),
                options.Value
                    .Select(option => new PendingTenant(option.TenantId, option.JoinedOnUtc))
                    .ToArray(),
                DateTimeOffset.UtcNow.Add(WebAuthDefaults.PendingLoginLifetime)));

            return Results.Redirect(QueryHelpers.AddQueryString("/select-tenant", "token", token));
        }

        Guid? tenantId = options.Value.Count == 1 ? options.Value[0].TenantId : null;
        Result<AuthenticationTokensResponse> login = await sender
            .SendAsync(
                new LoginCommand(
                    email,
                    password,
                    tenantId,
                    httpContext.Request.Headers.UserAgent.ToString(),
                    httpContext.Connection.RemoteIpAddress?.ToString()),
                httpContext.RequestAborted)
            .ConfigureAwait(false);

        if (login.IsFailure)
        {
            return RedirectToLoginError(MapError(login.Error));
        }

        return await SignInAndRedirectAsync(httpContext, login.Value, SafeReturnUrl(returnUrl))
            .ConfigureAwait(false);
    }

    private static async Task<IResult> SelectTenantAsync(HttpContext httpContext)
    {
        if (!await ValidateAntiforgeryAsync(httpContext).ConfigureAwait(false))
        {
            return Results.Redirect("/login?error=invalid_request");
        }

        PendingLoginStore pendingLogins = httpContext.RequestServices.GetRequiredService<PendingLoginStore>();
        IFormCollection form = await httpContext.Request.ReadFormAsync(httpContext.RequestAborted).ConfigureAwait(false);
        string? token = form["token"].ToString();
        PendingLogin? pending = pendingLogins.Load(token);

        if (pending is null)
        {
            return Results.Redirect("/login?error=selection_expired");
        }

        if (!Guid.TryParse(form["tenantId"].ToString(), out Guid tenantId)
            || !pending.Tenants.Any(option => option.TenantId == tenantId))
        {
            return Results.Redirect(QueryHelpers.AddQueryString("/select-tenant", "token", token));
        }

        ISender sender = httpContext.RequestServices.GetRequiredService<ISender>();
        Result<AuthenticationTokensResponse> login = await sender
            .SendAsync(
                new LoginCommand(
                    pending.Email,
                    pending.Password,
                    tenantId,
                    httpContext.Request.Headers.UserAgent.ToString(),
                    httpContext.Connection.RemoteIpAddress?.ToString()),
                httpContext.RequestAborted)
            .ConfigureAwait(false);

        if (login.IsFailure)
        {
            pendingLogins.Remove(token);
            return RedirectToLoginError(MapError(login.Error));
        }

        pendingLogins.Remove(token);
        return await SignInAndRedirectAsync(httpContext, login.Value, SafeReturnUrl(pending.ReturnUrl))
            .ConfigureAwait(false);
    }

    private static async Task<IResult> LogoutAsync(HttpContext httpContext)
    {
        if (!await ValidateAntiforgeryAsync(httpContext).ConfigureAwait(false))
        {
            return Results.Redirect("/login?error=invalid_request");
        }

        string? refreshToken = httpContext.Request.Cookies[WebAuthDefaults.RefreshTokenCookieName];
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            ISender sender = httpContext.RequestServices.GetRequiredService<ISender>();
            await sender
                .SendAsync(new LogoutCommand(refreshToken), httpContext.RequestAborted)
                .ConfigureAwait(false);
        }

        await httpContext.SignOutAsync(WebAuthDefaults.CookieScheme).ConfigureAwait(false);
        WebAuthCookieWriter.DeleteRefreshToken(httpContext);
        return Results.Redirect("/login?loggedOut=true");
    }

    private static async Task<IResult> SignInAndRedirectAsync(
        HttpContext httpContext,
        AuthenticationTokensResponse tokens,
        string returnUrl)
    {
        IAccessTokenValidator validator = httpContext.RequestServices.GetRequiredService<IAccessTokenValidator>();

        if (!validator.TryValidate(tokens.AccessToken, out ValidatedAccessToken token))
        {
            return RedirectToLoginError("login_failed");
        }

        DateTimeOffset expiresAtUtc = DateTimeOffset.UtcNow.Add(WebAuthDefaults.SessionLifetime);
        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = expiresAtUtc,
        };

        await httpContext
            .SignInAsync(WebAuthDefaults.CookieScheme, WebPrincipalFactory.Create(token), properties)
            .ConfigureAwait(false);

        WebAuthCookieWriter.AppendRefreshToken(httpContext, tokens.RefreshToken, expiresAtUtc);
        return Results.Redirect(SafeReturnUrl(returnUrl));
    }

    private static async Task<bool> ValidateAntiforgeryAsync(HttpContext httpContext)
    {
        if (!httpContext.Request.HasFormContentType)
        {
            return false;
        }

        try
        {
            IAntiforgery antiforgery = httpContext.RequestServices.GetRequiredService<IAntiforgery>();
            await antiforgery.ValidateRequestAsync(httpContext).ConfigureAwait(false);
            return true;
        }
        catch (AntiforgeryValidationException)
        {
            return false;
        }
    }

    private static IResult RedirectToLoginError(string error)
        => Results.Redirect(QueryHelpers.AddQueryString("/login", "error", error));

    private static string SafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl)
            || !returnUrl.StartsWith("/", StringComparison.Ordinal)
            || returnUrl.StartsWith("//", StringComparison.Ordinal)
            || returnUrl.StartsWith("/\\", StringComparison.Ordinal))
        {
            return "/";
        }

        return returnUrl;
    }

    private static string MapError(Error error) => error.Code switch
    {
        "Identity.Authentication.TooManyRequests" => "too_many_requests",
        "Identity.Authentication.InvalidCredentials" => "invalid_credentials",
        "Identity.Authentication.InvalidTenant" => "invalid_tenant",
        _ => "login_failed",
    };
}
