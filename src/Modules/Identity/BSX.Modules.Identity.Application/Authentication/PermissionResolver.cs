using BSX.Modules.Identity.Domain.Memberships;
using BSX.Modules.Identity.Domain.Roles;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.Authentication;

/// <summary>Resolves the effective permission set of a user within a tenant (ADR-012/ADR-017).</summary>
public sealed class PermissionResolver
{
    private readonly IRoleRepository _roles;

    public PermissionResolver(IRoleRepository roles) => _roles = roles;

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

        IReadOnlyCollection<Role> systemRoles = await _roles.ListByTenantAsync(null, cancellationToken).ConfigureAwait(false);

        foreach (Role role in systemRoles.Where(role => user.PlatformRoleIds.Contains(role.Id)))
        {
            foreach (Permission permission in role.Permissions)
            {
                permissions.Add(permission.Code);
            }
        }

        if (membership is not null && membership.Status == Domain.Enums.MembershipStatus.Active)
        {
            IReadOnlyCollection<Role> tenantRoles = await _roles.ListByTenantAsync(membership.TenantId, cancellationToken).ConfigureAwait(false);

            foreach (Role role in tenantRoles.Where(role => membership.RoleIds.Contains(role.Id)))
            {
                foreach (Permission permission in role.Permissions)
                {
                    permissions.Add(permission.Code);
                }
            }
        }

        return permissions;
    }
}
