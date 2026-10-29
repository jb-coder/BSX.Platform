using BSX.Modules.Identity.Domain.Memberships;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Identity.Infrastructure.Repositories;

internal sealed class TenantMembershipRepository : ITenantMembershipRepository
{
    private readonly IdentityDbContext _context;

    public TenantMembershipRepository(IdentityDbContext context) => _context = context;

    public Task<TenantMembership?> GetByIdAsync(MembershipId id, CancellationToken cancellationToken = default)
        => _context.TenantMemberships
            .Include(membership => membership.RoleIds)
            .FirstOrDefaultAsync(membership => membership.Id == id, cancellationToken);

    public Task<TenantMembership?> FindAsync(TenantId tenantId, UserId userId, CancellationToken cancellationToken = default)
        => _context.TenantMemberships
            .Include(membership => membership.RoleIds)
            .FirstOrDefaultAsync(
                membership => membership.TenantId == tenantId && membership.UserId == userId,
                cancellationToken);

    public Task<bool> ExistsAsync(TenantId tenantId, UserId userId, CancellationToken cancellationToken = default)
        => _context.TenantMemberships.AnyAsync(
            membership => membership.TenantId == tenantId && membership.UserId == userId,
            cancellationToken);

    public async Task<IReadOnlyCollection<TenantMembership>> ListByTenantAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default)
    {
        List<TenantMembership> memberships = await _context.TenantMemberships
            .Include(membership => membership.RoleIds)
            .Where(membership => membership.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return memberships;
    }

    public async Task<IReadOnlyCollection<TenantMembership>> ListByUserAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        List<TenantMembership> memberships = await _context.TenantMemberships
            .Include(membership => membership.RoleIds)
            .Where(membership => membership.UserId == userId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return memberships;
    }

    public Task TrackRoleChangesAsync(TenantMembership membership, CancellationToken cancellationToken = default)
    {
        foreach (var entry in _context.ChangeTracker.Entries()
            .Where(entry => entry.Metadata.Name.Contains("RoleIds", StringComparison.Ordinal)
                && entry.State == EntityState.Modified))
        {
            entry.State = EntityState.Added;
        }

        return Task.CompletedTask;
    }

    public async Task AddAsync(TenantMembership membership, CancellationToken cancellationToken = default)
        => await _context.TenantMemberships.AddAsync(membership, cancellationToken).ConfigureAwait(false);
}
