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
public sealed class IdentityPermissionCatalog : IPermissionCatalogSource
{
    /// <inheritdoc />
    public IReadOnlyCollection<PermissionDefinition> Permissions { get; } =
    [
        new(IdentityPermissionCodes.UsersRead, "Read users."),
        new(IdentityPermissionCodes.UsersCreate, "Create users."),
        new(IdentityPermissionCodes.UsersUpdate, "Update user names and email addresses."),
        new(IdentityPermissionCodes.UsersManageStatus, "Activate and deactivate users."),
        new(IdentityPermissionCodes.UsersAssignRole, "Assign roles and invite tenant memberships."),
        new(IdentityPermissionCodes.RolesRead, "Read roles."),
        new(IdentityPermissionCodes.RolesCreate, "Create roles."),
        new(IdentityPermissionCodes.RolesUpdate, "Update role names and descriptions."),
        new(IdentityPermissionCodes.RolesSetPermissions, "Change role permissions."),
        new(IdentityPermissionCodes.PermissionsRead, "Read the platform permission catalog."),
        new(IdentityPermissionCodes.MembershipsRead, "Read workspace memberships."),
        new(IdentityPermissionCodes.MembershipsManage, "Manage workspace memberships."),
    ];
}
