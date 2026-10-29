using Microsoft.AspNetCore.Authorization;

namespace BSX.Web.Authorization;

/// <summary>The permission-policy prefix understood by <see cref="PermissionPolicyProvider"/>.</summary>
public static class PermissionPolicies
{
    /// <summary>Policy prefix.</summary>
    public const string Prefix = "Permission:";

    /// <summary>Policy for reading users.</summary>
    public const string IdentityUsersRead = Prefix + "identity.users.read";

    /// <summary>Policy for creating users.</summary>
    public const string IdentityUsersCreate = Prefix + "identity.users.create";

    /// <summary>Policy for updating users.</summary>
    public const string IdentityUsersUpdate = Prefix + "identity.users.update";

    /// <summary>Policy for activating and deactivating users.</summary>
    public const string IdentityUsersManageStatus = Prefix + "identity.users.manage-status";

    /// <summary>Policy for assigning platform roles.</summary>
    public const string IdentityUsersAssignRole = Prefix + "identity.users.assign-role";

    /// <summary>Policy for reading roles.</summary>
    public const string IdentityRolesRead = Prefix + "identity.roles.read";

    /// <summary>Policy for creating roles.</summary>
    public const string IdentityRolesCreate = Prefix + "identity.roles.create";

    /// <summary>Policy for updating roles.</summary>
    public const string IdentityRolesUpdate = Prefix + "identity.roles.update";

    /// <summary>Policy for changing role permissions.</summary>
    public const string IdentityRolesSetPermissions = Prefix + "identity.roles.set-permissions";

    /// <summary>Policy for reading the permission catalog.</summary>
    public const string IdentityPermissionsRead = Prefix + "identity.permissions.read";

    /// <summary>Policy for reading workspace memberships.</summary>
    public const string IdentityMembershipsRead = Prefix + "identity.memberships.read";

    /// <summary>Policy for managing workspace memberships.</summary>
    public const string IdentityMembershipsManage = Prefix + "identity.memberships.manage";

    /// <summary>Policy for reading CRM customers.</summary>
    public const string CrmCustomersRead = Prefix + "crm.customers.read";

    /// <summary>Policy for creating CRM customers.</summary>
    public const string CrmCustomersCreate = Prefix + "crm.customers.create";

    /// <summary>Policy for updating CRM customers.</summary>
    public const string CrmCustomersUpdate = Prefix + "crm.customers.update";

    /// <summary>Policy for reading CRM organizations.</summary>
    public const string CrmOrganizationsRead = Prefix + "crm.organizations.read";

    /// <summary>Policy for creating CRM organizations.</summary>
    public const string CrmOrganizationsCreate = Prefix + "crm.organizations.create";

    /// <summary>Policy for updating CRM organizations.</summary>
    public const string CrmOrganizationsUpdate = Prefix + "crm.organizations.update";

    /// <summary>Policy for reading Sales leads.</summary>
    public const string SalesLeadsRead = Prefix + "sales.leads.read";

    /// <summary>Policy for creating Sales leads.</summary>
    public const string SalesLeadsCreate = Prefix + "sales.leads.create";

    /// <summary>Policy for updating Sales leads and their lifecycle.</summary>
    public const string SalesLeadsUpdate = Prefix + "sales.leads.update";

    /// <summary>Policy for reading Sales opportunities.</summary>
    public const string SalesOpportunitiesRead = Prefix + "sales.opportunities.read";

    /// <summary>Policy for creating Sales opportunities.</summary>
    public const string SalesOpportunitiesCreate = Prefix + "sales.opportunities.create";

    /// <summary>Policy for updating Sales opportunities and their lifecycle.</summary>
    public const string SalesOpportunitiesUpdate = Prefix + "sales.opportunities.update";

    /// <summary>Builds a policy name for a permission code.</summary>
    /// <param name="permission">The permission code.</param>
    public static string For(string permission) => Prefix + permission;
}

/// <summary>Requires a granted permission claim.</summary>
/// <param name="permission">The required permission code.</param>
public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    /// <summary>Gets the required permission code.</summary>
    public string Permission { get; } = string.IsNullOrWhiteSpace(permission)
        ? throw new ArgumentException("A permission is required.", nameof(permission))
        : permission;
}
