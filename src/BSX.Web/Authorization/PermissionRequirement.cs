using Microsoft.AspNetCore.Authorization;

namespace BSX.Web.Authorization;

/// <summary>The permission-policy prefix understood by <see cref="PermissionPolicyProvider"/>.</summary>
public static class PermissionPolicies
{
    /// <summary>Policy prefix.</summary>
    public const string Prefix = "Permission:";

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
