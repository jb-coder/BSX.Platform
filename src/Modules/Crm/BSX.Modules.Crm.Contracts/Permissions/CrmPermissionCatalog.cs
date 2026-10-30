using BSX.Contracts.Permissions;

namespace BSX.Modules.Crm.Contracts.Permissions;

/// <summary>
/// The CRM module's permission catalog. Identity consumes this source to reconcile the platform
/// system roles (ADR-018) without referencing CRM implementation types.
/// </summary>
public sealed class CrmPermissionCatalog : IPermissionCatalogSource
{
    /// <inheritdoc />
    public IReadOnlyCollection<PermissionDefinition> Permissions { get; } =
    [
        new(CrmPermissionCodes.CustomersRead, "Read customers.", PermissionScope.Tenant),
        new(CrmPermissionCodes.CustomersCreate, "Create customers.", PermissionScope.Tenant),
        new(CrmPermissionCodes.CustomersUpdate, "Update customers, contacts and addresses.", PermissionScope.Tenant),
        new(CrmPermissionCodes.OrganizationsRead, "Read organizations.", PermissionScope.Tenant),
        new(CrmPermissionCodes.OrganizationsCreate, "Create organizations.", PermissionScope.Tenant),
        new(CrmPermissionCodes.OrganizationsUpdate, "Update organizations.", PermissionScope.Tenant),
    ];
}
