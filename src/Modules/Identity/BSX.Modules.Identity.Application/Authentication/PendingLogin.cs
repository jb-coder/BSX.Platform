namespace BSX.Modules.Identity.Application.Authentication;

/// <summary>
/// A verified sign-in awaiting explicit workspace selection. The record deliberately contains no
/// password: the password is verified once by <c>DiscoverTenantOptionsCommand</c> and never retained.
/// </summary>
/// <param name="UserId">The verified user identifier.</param>
/// <param name="Email">The verified email address.</param>
/// <param name="ReturnUrl">The local return URL to use after sign-in.</param>
/// <param name="Tenants">The active tenant options available to the user.</param>
/// <param name="ExpiresAtUtc">When the pending sign-in expires.</param>
public sealed record PendingLogin(
    Guid UserId,
    string Email,
    string? ReturnUrl,
    IReadOnlyList<PendingTenant> Tenants,
    DateTimeOffset ExpiresAtUtc)
{
    /// <summary>
    /// Gets the non-secret selection handle used by the workspace chooser. The handle alone cannot
    /// complete a sign-in; the HttpOnly secret token is required.
    /// </summary>
    public string SelectionId { get; init; } = string.Empty;
}

/// <summary>A tenant option shown during workspace selection.</summary>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="JoinedOnUtc">When the membership became active.</param>
public sealed record PendingTenant(Guid TenantId, DateTimeOffset? JoinedOnUtc);

/// <summary>Opaque handles returned when a pending sign-in is stored.</summary>
/// <param name="SelectionId">The non-secret handle placed in the workspace chooser URL.</param>
/// <param name="SecretToken">The bearer secret stored only in an HttpOnly cookie.</param>
public sealed record PendingLoginHandle(string SelectionId, string SecretToken);
