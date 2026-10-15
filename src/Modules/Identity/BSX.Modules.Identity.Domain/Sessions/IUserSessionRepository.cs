using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Domain.Sessions;

/// <summary>Persistence contract for the <see cref="UserSession"/> aggregate.</summary>
public interface IUserSessionRepository
{
    /// <summary>Gets a session by identifier.</summary>
    /// <param name="id">The session identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<UserSession?> GetByIdAsync(SessionId id, CancellationToken cancellationToken = default);

    /// <summary>Finds an active session by its current refresh token hash.</summary>
    /// <param name="hash">The presented refresh token hash.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<UserSession?> FindByRefreshTokenAsync(RefreshTokenHash hash, CancellationToken cancellationToken = default);

    /// <summary>Lists the active sessions of a user.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<IReadOnlyCollection<UserSession>> ListActiveByUserAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>Lists all sessions of a user.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<IReadOnlyCollection<UserSession>> ListByUserAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>Adds a new session.</summary>
    /// <param name="session">The session.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task AddAsync(UserSession session, CancellationToken cancellationToken = default);
}
