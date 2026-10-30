using System.Collections.Concurrent;
using BSX.Modules.Identity.Domain.Sessions;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BSX.Web.IntegrationTests;

/// <summary>
/// Deterministically forces optimistic-concurrency conflicts on Identity rows for lockout tests.
/// It advances the stored row version immediately before EF issues the UPDATE, exactly as a
/// concurrent writer would, without requiring two live database connections.
/// </summary>
internal sealed class ForcedIdentityConcurrencySaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ConcurrentDictionary<Guid, Target> _targets = new();

    /// <summary>Forces a concurrency conflict on the next user-row update.</summary>
    /// <param name="userId">The target user.</param>
    /// <param name="times">The number of consecutive updates to conflict.</param>
    public void ForceUserConflict(Guid userId, int times = 1)
        => _targets[userId] = new Target(TargetKind.User, SessionConflictMode.None, times);

    /// <summary>Forces a concurrency conflict on the next session-row update.</summary>
    /// <param name="sessionId">The target session.</param>
    /// <param name="mode">Whether the simulated concurrent writer revokes or updates the session.</param>
    /// <param name="times">The number of consecutive updates to conflict.</param>
    public void ForceSessionConflict(Guid sessionId, SessionConflictMode mode, int times = 1)
        => _targets[sessionId] = new Target(TargetKind.Session, mode, times);

    /// <summary>Removes every armed target.</summary>
    public void Clear() => _targets.Clear();

    /// <inheritdoc />
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not IdentityDbContext context)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
        }

        foreach ((Guid id, Target target) in _targets.ToArray())
        {
            if (target.Remaining <= 0)
            {
                _targets.TryRemove(id, out _);
                continue;
            }

            bool matched = target.Kind switch
            {
                TargetKind.User => HasModifiedUser(context, id),
                TargetKind.Session => HasModifiedSession(context, id),
                _ => false,
            };

            if (!matched)
            {
                continue;
            }

            await ApplyConflictAsync(context, id, target, cancellationToken).ConfigureAwait(false);

            if (target.Remaining == int.MaxValue)
            {
                continue;
            }

            var updated = target with { Remaining = target.Remaining - 1 };

            if (updated.Remaining <= 0)
            {
                _targets.TryRemove(id, out _);
            }
            else
            {
                _targets[id] = updated;
            }
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
    }

    private static bool HasModifiedUser(IdentityDbContext context, Guid userId)
        => context.ChangeTracker.Entries<User>()
            .Any(entry => entry.State == EntityState.Modified && entry.Entity.Id.Value == userId);

    private static bool HasModifiedSession(IdentityDbContext context, Guid sessionId)
        => context.ChangeTracker.Entries<UserSession>()
            .Any(entry => entry.State == EntityState.Modified && entry.Entity.Id.Value == sessionId);

    private static Task ApplyConflictAsync(
        IdentityDbContext context,
        Guid id,
        Target target,
        CancellationToken cancellationToken)
    {
        return target.Kind switch
        {
            TargetKind.User => context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE \"users\" SET \"Version\" = \"Version\" + 1 WHERE \"Id\" = {id}",
                cancellationToken),
            TargetKind.Session when target.Mode == SessionConflictMode.Revoke => context.Database
                .ExecuteSqlInterpolatedAsync(
                    $"UPDATE \"user_sessions\" SET \"Version\" = \"Version\" + 1, \"Status\" = 'Revoked', \"RevokedOnUtc\" = {DateTimeOffset.UtcNow}, \"RevokedReason\" = 'concurrent_revoke' WHERE \"Id\" = {id}",
                    cancellationToken),
            TargetKind.Session => context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE \"user_sessions\" SET \"Version\" = \"Version\" + 1, \"LastRefreshedOnUtc\" = {DateTimeOffset.UtcNow} WHERE \"Id\" = {id}",
                cancellationToken),
            _ => Task.CompletedTask,
        };
    }

    /// <summary>The kind of concurrent write to simulate on a session row.</summary>
    public enum SessionConflictMode
    {
        /// <summary>No session write.</summary>
        None = 0,

        /// <summary>A concurrent writer revokes the session.</summary>
        Revoke = 1,

        /// <summary>A concurrent writer updates the session (for example, refreshes it).</summary>
        Update = 2,
    }

    private enum TargetKind
    {
        User = 0,
        Session = 1,
    }

    private sealed record Target(TargetKind Kind, SessionConflictMode Mode, int Remaining);
}
