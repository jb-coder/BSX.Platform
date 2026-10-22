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
        new(CrmPermissionCodes.CustomersRead, "Read customers."),
        new(CrmPermissionCodes.CustomersCreate, "Create customers."),
        new(CrmPermissionCodes.CustomersUpdate, "Update customers, contacts and addresses."),
        new(CrmPermissionCodes.OrganizationsRead, "Read organizations."),
        new(CrmPermissionCodes.OrganizationsCreate, "Create organizations."),
        new(CrmPermissionCodes.OrganizationsUpdate, "Update organizations."),
    ];
}
