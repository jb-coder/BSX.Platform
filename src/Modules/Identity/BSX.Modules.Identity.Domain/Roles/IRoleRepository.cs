using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Domain.Roles;

/// <summary>Persistence contract for the <see cref="Role"/> aggregate.</summary>
public interface IRoleRepository
{
    /// <summary>Gets a role by identifier.</summary>
    /// <param name="id">The role identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<Role?> GetByIdAsync(RoleId id, CancellationToken cancellationToken = default);

    /// <summary>Determines whether a role with the given name exists in the tenant (unique per tenant, ADR-013).</summary>
    /// <param name="tenantId">The tenant identifier; null for a system role.</param>
    /// <param name="name">The role name.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<bool> ExistsByNameAsync(TenantId? tenantId, RoleName name, CancellationToken cancellationToken = default);

    /// <summary>Lists roles for a tenant; null lists system roles.</summary>
    /// <param name="tenantId">The tenant identifier; null for system roles.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<IReadOnlyCollection<Role>> ListByTenantAsync(TenantId? tenantId, CancellationToken cancellationToken = default);

    /// <summary>Adds a new role.</summary>
    /// <param name="role">The role.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task AddAsync(Role role, CancellationToken cancellationToken = default);
}
