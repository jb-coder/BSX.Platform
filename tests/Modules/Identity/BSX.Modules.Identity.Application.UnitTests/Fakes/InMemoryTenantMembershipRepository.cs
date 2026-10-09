using BSX.Modules.Identity.Domain.Memberships;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.UnitTests.Fakes;

/// <summary>In-memory <see cref="ITenantMembershipRepository"/> for handler tests.</summary>
internal sealed class InMemoryTenantMembershipRepository : ITenantMembershipRepository
{
    private readonly List<TenantMembership> _memberships = [];

    public IReadOnlyCollection<TenantMembership> Memberships => _memberships;

    public void Seed(params TenantMembership[] memberships) => _memberships.AddRange(memberships);

    public Task<TenantMembership?> GetByIdAsync(MembershipId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_memberships.Find(membership => membership.Id == id));

    public Task<TenantMembership?> FindAsync(TenantId tenantId, UserId userId, CancellationToken cancellationToken = default)
        => Task.FromResult(_memberships.Find(membership => membership.TenantId == tenantId && membership.UserId == userId));

    public Task<bool> ExistsAsync(TenantId tenantId, UserId userId, CancellationToken cancellationToken = default)
        => Task.FromResult(_memberships.Exists(membership => membership.TenantId == tenantId && membership.UserId == userId));

    public Task<IReadOnlyCollection<TenantMembership>> ListByUserAsync(UserId userId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyCollection<TenantMembership>>(_memberships.Where(membership => membership.UserId == userId).ToList());

    public Task AddAsync(TenantMembership membership, CancellationToken cancellationToken = default)
    {
        _memberships.Add(membership);
        return Task.CompletedTask;
    }
}
