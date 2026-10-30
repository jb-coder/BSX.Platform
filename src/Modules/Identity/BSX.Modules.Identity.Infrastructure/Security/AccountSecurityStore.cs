using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.Sessions;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BSX.Modules.Identity.Infrastructure.Security;

/// <summary>
/// Persists account-security state in its own scope and transaction (ADR-022). The write is
/// independent of the caller's command transaction, so a failed login command cannot roll back the
/// failure counter or the lockout.
/// </summary>
/// <remarks>
/// Lockout durability never depends on session cleanup. The user security state (failure count,
/// lockout and security stamp) is committed first; active sessions are revoked afterwards on a
/// best-effort basis. A concurrency conflict while revoking a session therefore cannot roll back
/// the lockout (ADR-022 amendment).
/// </remarks>
internal sealed class AccountSecurityStore : IAccountSecurityStore
{
    private const int MaxPersistenceAttempts = 3;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly LockoutOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AccountSecurityStore> _logger;

    /// <summary>Initializes a new instance of the <see cref="AccountSecurityStore"/> class.</summary>
    /// <param name="scopeFactory">The scope factory used for the independent write.</param>
    /// <param name="options">The lockout policy.</param>
    /// <param name="timeProvider">The time provider.</param>
    /// <param name="logger">The logger.</param>
    public AccountSecurityStore(
        IServiceScopeFactory scopeFactory,
        IOptions<LockoutOptions> options,
        TimeProvider timeProvider,
        ILogger<AccountSecurityStore> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task RecordAccessFailureAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        DateTimeOffset? lockedAtUtc = await PersistFailureAsync(userId, cancellationToken).ConfigureAwait(false);

        if (lockedAtUtc is null)
        {
            return;
        }

        // The lockout and security stamp are already durable. Session revocation is cleanup: it is
        // retried independently and can never undo the lockout.
        await RevokeActiveSessionsBestEffortAsync(userId, lockedAtUtc.Value).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RevokeSessionAsync(SessionId sessionId, string reason, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var sessions = scope.ServiceProvider.GetRequiredService<IUserSessionRepository>();

        UserSession? session = await sessions.GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false);

        if (session is null)
        {
            return;
        }

        session.Revoke(_timeProvider.GetUtcNow(), reason);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Commits the failed-attempt counter and, when the threshold is reached, the lockout and the
    /// rotated security stamp. The user row is the only aggregate in this transaction, so a session
    /// concurrency conflict cannot roll it back. User-row conflicts are retried against fresh state.
    /// </summary>
    /// <returns>The lockout timestamp when this attempt transitioned the account to locked.</returns>
    private async Task<DateTimeOffset?> PersistFailureAsync(UserId userId, CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= MaxPersistenceAttempts; attempt++)
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

            await using var transaction = await context.Database
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false);

            if (context.Database.IsNpgsql())
            {
                // Serialize concurrent failures for the same account so no increment is lost.
                await context.Database
                    .ExecuteSqlInterpolatedAsync(
                        $"SELECT pg_advisory_xact_lock(hashtext({userId.Value.ToString()}))",
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            User? user = await context.Users
                .Include(candidate => candidate.Credentials)
                .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
                .ConfigureAwait(false);

            if (user is null)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return null;
            }

            DateTimeOffset nowUtc = _timeProvider.GetUtcNow();

            if (user.IsLockedOut(nowUtc))
            {
                // Already locked; do not extend the lockout or count further attempts.
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return null;
            }

            if (user.Status is UserStatus.Disabled or UserStatus.Pending)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return null;
            }

            if (user.Status == UserStatus.Locked)
            {
                // The lockout window elapsed; the next attempt starts from a clean counter.
                user.Unlock();
            }

            user.RecordAccessFailure(nowUtc, _options.EffectiveMaxFailedAttempts, _options.EffectiveLockoutDuration);

            bool nowLocked = user.IsLockedOut(nowUtc);

            try
            {
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxPersistenceAttempts)
            {
                // A concurrent writer changed the user row. This transaction rolled back, so the
                // increment was not persisted; reload and re-apply it against the fresh state.
                _logger.LogWarning(
                    "Concurrent modification while recording a failed sign-in for account {UserId}; retrying (attempt {Attempt}/{MaxAttempts}).",
                    userId.Value,
                    attempt,
                    MaxPersistenceAttempts);
                continue;
            }

            if (nowLocked)
            {
                _logger.LogWarning(
                    "Account {UserId} was locked after {FailureCount} consecutive failed sign-in attempts.",
                    user.Id.Value,
                    user.AccessFailedCount);
                return nowUtc;
            }

            return null;
        }

        // The final attempt rethrows its concurrency exception; this guard satisfies the compiler.
        throw new InvalidOperationException("The account security state could not be persisted.");
    }

    /// <summary>
    /// Revokes every active session for a locked account. Failures are logged and retried, never
    /// propagated: the lockout is already committed and authorization revalidation rejects any
    /// session that retains the pre-lockout security stamp.
    /// </summary>
    private async Task RevokeActiveSessionsBestEffortAsync(UserId userId, DateTimeOffset nowUtc)
    {
        for (int attempt = 1; attempt <= MaxPersistenceAttempts; attempt++)
        {
            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                var sessions = scope.ServiceProvider.GetRequiredService<IUserSessionRepository>();

                await using var transaction = await context.Database
                    .BeginTransactionAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                IReadOnlyCollection<UserSession> activeSessions = await sessions
                    .ListActiveByUserAsync(userId, CancellationToken.None)
                    .ConfigureAwait(false);

                if (activeSessions.Count == 0)
                {
                    await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                    return;
                }

                foreach (UserSession session in activeSessions)
                {
                    session.Revoke(nowUtc, "account_locked");
                }

                await context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
                await transaction.CommitAsync(CancellationToken.None).ConfigureAwait(false);
                return;
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxPersistenceAttempts)
            {
                // A session changed concurrently. Reloading the active set makes the retry
                // idempotent: sessions revoked by the other writer are simply skipped.
                _logger.LogWarning(
                    "Concurrent modification while revoking sessions for locked account {UserId}; retrying (attempt {Attempt}/{MaxAttempts}).",
                    userId.Value,
                    attempt,
                    MaxPersistenceAttempts);
            }
            catch (DbUpdateConcurrencyException)
            {
                // The lockout and stamp rotation are durable. Leaving a session active is safe
                // because revalidation and refresh reject the stale security stamp.
                _logger.LogError(
                    "Could not revoke all sessions for locked account {UserId} after {Attempts} attempts; the lockout remains in effect.",
                    userId.Value,
                    MaxPersistenceAttempts);
                return;
            }
        }
    }
}
