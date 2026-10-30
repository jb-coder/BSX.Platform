using BSX.Contracts.Permissions;
using BSX.Modules.Identity.Domain.Memberships;
using BSX.Modules.Identity.Domain.Roles;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.Authentication;

/// <summary>
/// Resolves the effective permission set of a user within a tenant (ADR-012/ADR-017).
/// </summary>
/// <remarks>
/// This is the final authorization backstop for permission scope (ADR-020). Regardless of what a
/// stored role contains, a tenant role can only contribute tenant-scoped permissions, and an
/// unknown code contributes nothing. This keeps authorization safe even if a role row was written
/// outside the application (for example through direct repository or database manipulation).
/// </remarks>
public sealed class PermissionResolver
{
    private readonly IRoleRepository _roles;
    private readonly PermissionCatalog _catalog;

    /// <summary>Initializes a new instance of the <see cref="PermissionResolver"/> class.</summary>
    /// <param name="roles">The role repository.</param>
    /// <param name="catalog">The authoritative permission catalog used to resolve scopes.</param>
    public PermissionResolver(IRoleRepository roles, PermissionCatalog catalog)
    {
        _roles = roles;
        _catalog = catalog;
    }

    /// <summary>Resolves the union of platform-role and tenant-membership-role permissions.</summary>
    /// <param name="user">The user.</param>
    /// <param name="membership">The active membership, if any.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    public async Task<IReadOnlyCollection<string>> ResolveAsync(
        User user,
        TenantMembership? membership,
        CancellationToken cancellationToken = default)
    {
        var permissions = new HashSet<string>(StringComparer.Ordinal);

        // Platform roles may grant both platform and tenant permissions. Unknown codes are dropped so
        // a stale or tampered catalog entry can never become effective.
        IReadOnlyCollection<Role> platformRoles = await _roles.ListByTenantAsync(null, cancellationToken).ConfigureAwait(false);

        foreach (Role role in platformRoles.Where(role => user.PlatformRoleIds.Contains(role.Id)))
        {
            foreach (Permission permission in role.Permissions)
            {
                if (_catalog.Contains(permission.Code))
                {
                    permissions.Add(permission.Code);
                }
            }
        }

        if (membership is not null && membership.Status == Domain.Enums.MembershipStatus.Active)
        {
            IReadOnlyCollection<Role> tenantRoles = await _roles.ListByTenantAsync(membership.TenantId, cancellationToken).ConfigureAwait(false);

            foreach (Role role in tenantRoles.Where(role => membership.RoleIds.Contains(role.Id)))
            {
                foreach (Permission permission in role.Permissions)
                {
                    // Tenant roles can never grant platform-scoped capabilities (ADR-020).
                    if (_catalog.ScopeOf(permission.Code) == PermissionScope.Tenant)
                    {
                        permissions.Add(permission.Code);
                    }
                }
            }
        }

        return permissions;
    }
}
