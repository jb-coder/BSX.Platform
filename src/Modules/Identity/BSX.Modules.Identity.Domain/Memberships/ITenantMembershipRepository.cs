using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Domain.Memberships;

/// <summary>Persistence contract for the <see cref="TenantMembership"/> aggregate.</summary>
public interface ITenantMembershipRepository
{
    /// <summary>Gets a membership by identifier.</summary>
    /// <param name="id">The membership identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<TenantMembership?> GetByIdAsync(MembershipId id, CancellationToken cancellationToken = default);

    /// <summary>Finds a membership for a user in a tenant.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<TenantMembership?> FindAsync(TenantId tenantId, UserId userId, CancellationToken cancellationToken = default);

    /// <summary>Determines whether a membership exists for a user in a tenant.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<bool> ExistsAsync(TenantId tenantId, UserId userId, CancellationToken cancellationToken = default);

    /// <summary>Lists the memberships in a tenant.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<IReadOnlyCollection<TenantMembership>> ListByTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default);

    /// <summary>Lists the memberships of a user.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<IReadOnlyCollection<TenantMembership>> ListByUserAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>Marks newly assigned membership roles as inserts for EF Core.</summary>
    /// <param name="membership">The tracked membership.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task TrackRoleChangesAsync(TenantMembership membership, CancellationToken cancellationToken = default);

    /// <summary>Adds a new membership.</summary>
    /// <param name="membership">The membership.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task AddAsync(TenantMembership membership, CancellationToken cancellationToken = default);
}
