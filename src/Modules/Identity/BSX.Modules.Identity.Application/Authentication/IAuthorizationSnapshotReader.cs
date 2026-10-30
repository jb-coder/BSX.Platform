namespace BSX.Modules.Identity.Application.Authentication;

/// <summary>
/// Reads the authoritative authorization state for a session. Implementations must fail closed: a
/// missing, revoked, expired, deactivated or tenant-mismatched principal resolves to
/// <see langword="null"/> (ADR-021).
/// </summary>
public interface IAuthorizationSnapshotReader
{
    /// <summary>
    /// Reads the current authorization state for a session.
    /// </summary>
    /// <param name="userId">The user identifier from the principal.</param>
    /// <param name="sessionId">The session identifier from the principal.</param>
    /// <param name="tenantId">The active tenant from the principal, if any.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The authoritative snapshot, or <see langword="null"/> when access must be denied.</returns>
    Task<AuthorizationSnapshot?> ReadAsync(
        Guid userId,
        Guid sessionId,
        Guid? tenantId,
        CancellationToken cancellationToken = default);
}
