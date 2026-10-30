namespace BSX.Modules.Identity.Application.Authentication;

/// <summary>
/// The authoritative authorization state of a principal at a point in time (ADR-021). It is built
/// from the session, account, membership and freshly resolved role permissions, never from cached
/// token claims.
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="TenantId">The active tenant, or null for a platform-only principal.</param>
/// <param name="SessionId">The session identifier.</param>
/// <param name="Email">The current email address.</param>
/// <param name="Name">The current display name.</param>
/// <param name="SecurityStamp">The current security stamp.</param>
/// <param name="Permissions">The freshly resolved effective permissions.</param>
public sealed record AuthorizationSnapshot(
    Guid UserId,
    Guid? TenantId,
    Guid SessionId,
    string Email,
    string Name,
    string SecurityStamp,
    IReadOnlyCollection<string> Permissions);
