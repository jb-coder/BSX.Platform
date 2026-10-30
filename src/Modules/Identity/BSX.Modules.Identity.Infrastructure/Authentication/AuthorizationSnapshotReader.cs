using BSX.Modules.Identity.Application.Authentication;
using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.Memberships;
using BSX.Modules.Identity.Domain.Sessions;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Identity.Infrastructure.Authentication;

/// <summary>
/// Reads the authoritative authorization state directly from the Identity store (ADR-021). It
/// deliberately bypasses cached token claims so a revoked session, deactivated user or removed
/// membership is observed on the next operation.
/// </summary>
internal sealed class AuthorizationSnapshotReader : IAuthorizationSnapshotReader
{
    private readonly IdentityDbContext _context;
    private readonly PermissionResolver _permissions;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="AuthorizationSnapshotReader"/> class.</summary>
    /// <param name="context">The Identity persistence context.</param>
    /// <param name="permissions">The effective permission resolver.</param>
    /// <param name="timeProvider">The time provider.</param>
    public AuthorizationSnapshotReader(
        IdentityDbContext context,
        PermissionResolver permissions,
        TimeProvider timeProvider)
    {
        _context = context;
        _permissions = permissions;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<AuthorizationSnapshot?> ReadAsync(
        Guid userId,
        Guid sessionId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || sessionId == Guid.Empty)
        {
            return null;
        }

        DateTimeOffset nowUtc = _timeProvider.GetUtcNow();

        UserSession? session = await _context.Sessions
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == SessionId.From(sessionId), cancellationToken)
            .ConfigureAwait(false);

        if (session is null
            || session.UserId.Value != userId
            || !session.IsActive(nowUtc)
            || session.TenantId?.Value != tenantId)
        {
            return null;
        }

        User? user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == UserId.From(userId), cancellationToken)
            .ConfigureAwait(false);

        if (user is null
            || user.Status != UserStatus.Active
            || !string.Equals(session.SecurityStamp, user.SecurityStamp.Value, StringComparison.Ordinal))
        {
            return null;
        }

        TenantMembership? membership = null;

        if (session.TenantId is { } sessionTenantId)
        {
            membership = await _context.TenantMemberships
                .AsNoTracking()
                .Include(candidate => candidate.RoleIds)
                .FirstOrDefaultAsync(
                    candidate => candidate.TenantId == sessionTenantId && candidate.UserId == user.Id,
                    cancellationToken)
                .ConfigureAwait(false);

            if (membership is null || membership.Status != MembershipStatus.Active)
            {
                return null;
            }
        }

        IReadOnlyCollection<string> permissions = await _permissions
            .ResolveAsync(user, membership, cancellationToken)
            .ConfigureAwait(false);

        return new AuthorizationSnapshot(
            user.Id.Value,
            session.TenantId?.Value,
            session.Id.Value,
            user.Email.Value,
            user.Name.Value,
            user.SecurityStamp.Value,
            permissions);
    }
}
