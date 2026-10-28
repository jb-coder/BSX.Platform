namespace BSX.Web.Authentication;

/// <summary>A pending sign-in awaiting explicit tenant selection.</summary>
/// <param name="Email">The verified email. Held only in a protected, short-lived cookie.</param>
/// <param name="Password">The verified password. Held only in a protected, short-lived cookie.</param>
/// <param name="ReturnUrl">The local return URL to use after sign-in.</param>
/// <param name="Tenants">The active tenant options available to the user.</param>
/// <param name="ExpiresAtUtc">When the pending sign-in expires.</param>
public sealed record PendingLogin(
    string Email,
    string Password,
    string? ReturnUrl,
    IReadOnlyList<PendingTenant> Tenants,
    DateTimeOffset ExpiresAtUtc);

/// <summary>A tenant option shown on the workspace chooser.</summary>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="JoinedOnUtc">When the membership became active.</param>
public sealed record PendingTenant(Guid TenantId, DateTimeOffset? JoinedOnUtc);
