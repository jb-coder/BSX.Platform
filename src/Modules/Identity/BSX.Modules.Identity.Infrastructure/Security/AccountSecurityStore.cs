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
internal sealed class AccountSecurityStore : IAccountSecurityStore
{
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
        using IServiceScope scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var sessions = scope.ServiceProvider.GetRequiredService<IUserSessionRepository>();

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
            return;
        }

        DateTimeOffset nowUtc = _timeProvider.GetUtcNow();

        if (user.IsLockedOut(nowUtc))
        {
            // Already locked; do not extend the lockout or count further attempts.
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        if (user.Status is UserStatus.Disabled or UserStatus.Pending)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        if (user.Status == UserStatus.Locked)
        {
            // The lockout window elapsed; the next attempt starts from a clean counter.
            user.Unlock();
        }

        bool wasLocked = user.IsLockedOut(nowUtc);

        user.RecordAccessFailure(nowUtc, _options.EffectiveMaxFailedAttempts, _options.EffectiveLockoutDuration);

        bool nowLocked = user.IsLockedOut(nowUtc);

        if (!wasLocked && nowLocked)
        {
            IReadOnlyCollection<UserSession> activeSessions = await sessions
                .ListActiveByUserAsync(user.Id, cancellationToken)
                .ConfigureAwait(false);

            foreach (UserSession session in activeSessions)
            {
                session.Revoke(nowUtc, "account_locked");
            }

            _logger.LogWarning(
                "Account {UserId} was locked after {FailureCount} consecutive failed sign-in attempts.",
                user.Id.Value,
                user.AccessFailedCount);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
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
}
