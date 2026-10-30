using BSX.Contracts.Permissions;

namespace BSX.Modules.Sales.Contracts.Permissions;

/// <summary>
/// The Sales module's permission catalog. Identity consumes this source to seed the platform
/// system roles (ADR-018); it does not reference the Sales implementation.
/// </summary>
public sealed class SalesPermissionCatalog : IPermissionCatalogSource
{
    /// <inheritdoc />
    public IReadOnlyCollection<PermissionDefinition> Permissions { get; } =
    [
        new(SalesPermissionCodes.LeadsRead, "Read leads.", PermissionScope.Tenant),
        new(SalesPermissionCodes.LeadsCreate, "Create leads.", PermissionScope.Tenant),
        new(SalesPermissionCodes.LeadsUpdate, "Update leads and advance their lifecycle.", PermissionScope.Tenant),
        new(SalesPermissionCodes.OpportunitiesRead, "Read opportunities.", PermissionScope.Tenant),
        new(SalesPermissionCodes.OpportunitiesCreate, "Create opportunities from qualified leads.", PermissionScope.Tenant),
        new(SalesPermissionCodes.OpportunitiesUpdate, "Update opportunities and advance their lifecycle.", PermissionScope.Tenant),
    ];
}
