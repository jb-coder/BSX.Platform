using BSX.Contracts.Permissions;

namespace BSX.Modules.Identity.Application.Common;

/// <summary>Stable Identity permission codes published to the platform permission catalog (ADR-012).</summary>
public static class IdentityPermissionCodes
{
    /// <summary>Read users.</summary>
    public const string UsersRead = "identity.users.read";

    /// <summary>Create users.</summary>
    public const string UsersCreate = "identity.users.create";

    /// <summary>Update user profile fields.</summary>
    public const string UsersUpdate = "identity.users.update";

    /// <summary>Activate and deactivate users.</summary>
    public const string UsersManageStatus = "identity.users.manage-status";

    /// <summary>Assign platform roles and invite memberships.</summary>
    public const string UsersAssignRole = "identity.users.assign-role";

    /// <summary>Read roles.</summary>
    public const string RolesRead = "identity.roles.read";

    /// <summary>Create roles.</summary>
    public const string RolesCreate = "identity.roles.create";

    /// <summary>Update role names and descriptions.</summary>
    public const string RolesUpdate = "identity.roles.update";

    /// <summary>Replace role permissions.</summary>
    public const string RolesSetPermissions = "identity.roles.set-permissions";

    /// <summary>Read the permission catalog.</summary>
    public const string PermissionsRead = "identity.permissions.read";

    /// <summary>Read workspace memberships.</summary>
    public const string MembershipsRead = "identity.memberships.read";

    /// <summary>Manage workspace memberships.</summary>
    public const string MembershipsManage = "identity.memberships.manage";
}

/// <summary>
/// The Identity module's permission catalog. It is registered as a platform catalog source so
/// system roles stay correct as the permission set evolves (ADR-018).
/// </summary>
/// <remarks>
/// Scope classification follows the data each capability operates on (ADR-020):
/// <list type="bullet">
/// <item><description><c>identity.users.*</c> operate on the global user directory and are platform-scoped.</description></item>
/// <item><description><c>identity.roles.*</c>, <c>identity.memberships.*</c> and <c>identity.permissions.read</c>
/// are tenant-confined by the tenant guard (role writes) or expose only read-only contract metadata,
/// and are tenant-scoped.</description></item>
/// </list>
/// </remarks>
public sealed class IdentityPermissionCatalog : IPermissionCatalogSource
{
    /// <inheritdoc />
    public IReadOnlyCollection<PermissionDefinition> Permissions { get; } =
    [
        new(IdentityPermissionCodes.UsersRead, "Read users.", PermissionScope.Platform),
        new(IdentityPermissionCodes.UsersCreate, "Create users.", PermissionScope.Platform),
        new(IdentityPermissionCodes.UsersUpdate, "Update user names and email addresses.", PermissionScope.Platform),
        new(IdentityPermissionCodes.UsersManageStatus, "Activate and deactivate users.", PermissionScope.Platform),
        new(IdentityPermissionCodes.UsersAssignRole, "Assign platform roles and invite tenant memberships.", PermissionScope.Platform),
        new(IdentityPermissionCodes.RolesRead, "Read roles.", PermissionScope.Tenant),
        new(IdentityPermissionCodes.RolesCreate, "Create roles.", PermissionScope.Tenant),
        new(IdentityPermissionCodes.RolesUpdate, "Update role names and descriptions.", PermissionScope.Tenant),
        new(IdentityPermissionCodes.RolesSetPermissions, "Change role permissions.", PermissionScope.Tenant),
        new(IdentityPermissionCodes.PermissionsRead, "Read the platform permission catalog.", PermissionScope.Tenant),
        new(IdentityPermissionCodes.MembershipsRead, "Read workspace memberships.", PermissionScope.Tenant),
        new(IdentityPermissionCodes.MembershipsManage, "Manage workspace memberships.", PermissionScope.Tenant),
    ];
}
