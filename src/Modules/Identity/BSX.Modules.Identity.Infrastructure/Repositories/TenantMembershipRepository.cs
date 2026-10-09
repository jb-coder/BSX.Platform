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
        => _context.TenantMemberships.FirstOrDefaultAsync(membership => membership.Id == id, cancellationToken);

    public Task<TenantMembership?> FindAsync(TenantId tenantId, UserId userId, CancellationToken cancellationToken = default)
        => _context.TenantMemberships.FirstOrDefaultAsync(
            membership => membership.TenantId == tenantId && membership.UserId == userId,
            cancellationToken);

    public Task<bool> ExistsAsync(TenantId tenantId, UserId userId, CancellationToken cancellationToken = default)
        => _context.TenantMemberships.AnyAsync(
            membership => membership.TenantId == tenantId && membership.UserId == userId,
            cancellationToken);

    public async Task<IReadOnlyCollection<TenantMembership>> ListByUserAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        List<TenantMembership> memberships = await _context.TenantMemberships
            .Where(membership => membership.UserId == userId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return memberships;
    }

    public async Task AddAsync(TenantMembership membership, CancellationToken cancellationToken = default)
        => await _context.TenantMemberships.AddAsync(membership, cancellationToken).ConfigureAwait(false);
}
