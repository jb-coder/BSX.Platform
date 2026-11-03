using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Identity.Application.Authentication.CompletePasswordReset;
using BSX.Modules.Identity.Application.Authentication.RequestPasswordReset;
using BSX.SharedKernel.Results;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.WebUtilities;

namespace BSX.Web.Authentication;

/// <summary>
/// Thin web adapters for the password-recovery pages. The request endpoint is deliberately
/// enumeration-safe: unknown and existing accounts produce the same redirect, while unavailable or
/// rate-limited delivery produces a non-account-specific message.
/// </summary>
public static class WebPasswordResetEndpoints
{
    /// <summary>Maps the password-reset request and completion endpoints.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The same builder for chaining.</returns>
    public static IEndpointRouteBuilder MapWebPasswordResetEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost("/auth/password-reset/request", (Delegate)RequestAsync).AllowAnonymous();
        endpoints.MapPost("/auth/password-reset/complete", (Delegate)CompleteAsync).AllowAnonymous();

        return endpoints;
    }

    private static async Task<IResult> RequestAsync(HttpContext httpContext)
    {
        if (!await WebAuthEndpoints.ValidateAntiforgeryAsync(httpContext).ConfigureAwait(false))
        {
            return RedirectToForgotError("invalid_request");
        }

        IFormCollection form = await httpContext.Request
            .ReadFormAsync(httpContext.RequestAborted)
            .ConfigureAwait(false);

        string email = form["email"].ToString().Trim();

        if (string.IsNullOrWhiteSpace(email))
        {
            return RedirectToForgotError("invalid_email");
        }

        ISender sender = httpContext.RequestServices.GetRequiredService<ISender>();
        Result<RequestPasswordResetResponse> result = await sender
            .SendAsync(new RequestPasswordResetCommand(email), httpContext.RequestAborted)
            .ConfigureAwait(false);

        if (result.IsSuccess)
        {
            // Same response for known and unknown accounts.
            return Results.Redirect("/forgot-password?sent=true");
        }

        return RedirectToForgotError(MapRequestError(result.Error));
    }

    private static async Task<IResult> CompleteAsync(HttpContext httpContext)
    {
        if (!await WebAuthEndpoints.ValidateAntiforgeryAsync(httpContext).ConfigureAwait(false))
        {
            return Results.Redirect("/reset-password?error=invalid_request");
        }

        IFormCollection form = await httpContext.Request
            .ReadFormAsync(httpContext.RequestAborted)
            .ConfigureAwait(false);

        string token = form["token"].ToString();
        string newPassword = form["newPassword"].ToString();
        string confirmPassword = form["confirmPassword"].ToString();

        if (string.IsNullOrWhiteSpace(token))
        {
            return Results.Redirect("/reset-password?error=invalid_token");
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            return RedirectResetError(token, "password_required");
        }

        if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
        {
            return RedirectResetError(token, "password_mismatch");
        }

        ISender sender = httpContext.RequestServices.GetRequiredService<ISender>();
        Result result = await sender
            .SendAsync(new CompletePasswordResetCommand(token, newPassword), httpContext.RequestAborted)
            .ConfigureAwait(false);

        if (result.IsSuccess)
        {
            // The reset command revoked every Identity session. Clear browser artifacts as well so
            // an already-open browser returns to the sign-in experience immediately.
            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                await httpContext.SignOutAsync(WebAuthDefaults.CookieScheme).ConfigureAwait(false);
            }

            WebAuthCookieWriter.DeleteRefreshToken(httpContext);
            WebAuthCookieWriter.DeletePendingLogin(httpContext);
            return Results.Redirect("/login?reset=success");
        }

        return RedirectResetError(token, MapCompleteError(result.Error));
    }

    private static IResult RedirectToForgotError(string error)
        => Results.Redirect(QueryHelpers.AddQueryString("/forgot-password", "error", error));

    private static IResult RedirectResetError(string token, string error)
    {
        if (error is "expired" or "used" or "invalid_token")
        {
            return Results.Redirect(QueryHelpers.AddQueryString("/reset-password", "error", error));
        }

        string withToken = QueryHelpers.AddQueryString("/reset-password", "token", token);
        return Results.Redirect(QueryHelpers.AddQueryString(withToken, "error", error));
    }

    private static string MapRequestError(Error error)
    {
        if (error.Type == ErrorType.Validation)
        {
            return "invalid_email";
        }

        return error.Code switch
        {
            "Identity.PasswordReset.Unavailable" => "unavailable",
            "Identity.Authentication.TooManyRequests" => "too_many_requests",
            _ => "request_failed",
        };
    }

    private static string MapCompleteError(Error error)
    {
        if (error.Type == ErrorType.Validation)
        {
            return "password_rule";
        }

        return error.Code switch
        {
            "Identity.User.PasswordResetTokenExpired" => "expired",
            "Identity.User.PasswordResetTokenUsed" => "used",
            "Identity.User.PasswordResetTokenInvalid" => "invalid_token",
            "Identity.User.PasswordResetTokenMissing" => "invalid_token",
            "Identity.Concurrency.Conflict" => "reset_failed",
            _ => "reset_failed",
        };
    }
}
