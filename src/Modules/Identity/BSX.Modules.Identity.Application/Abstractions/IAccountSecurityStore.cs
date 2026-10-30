using BSX.Modules.Identity.Domain.Sessions;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.Abstractions;

/// <summary>
/// Persists account-security state independently of the caller's command transaction (ADR-022).
/// Implementations must commit their writes even when the surrounding authentication command
/// returns a failed result.
/// </summary>
public interface IAccountSecurityStore
{
    /// <summary>
    /// Records a failed credential attempt durably. When the threshold is reached the account is
    /// locked, the security stamp is rotated and all active sessions are revoked.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task RecordAccessFailureAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a session durably. Used when a refresh request is rejected so the revocation survives
    /// the failed command transaction.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="reason">The non-sensitive revocation reason.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task RevokeSessionAsync(SessionId sessionId, string reason, CancellationToken cancellationToken = default);
}
