using BSX.Contracts.Permissions;

namespace BSX.Modules.Inventory.Application.Common;

/// <summary>Stable permission codes for the Inventory module (ADR-012).</summary>
public static class InventoryPermissions
{
    /// <summary>Read products.</summary>
    public const string ProductsRead = "inventory.products.read";

    /// <summary>Create products.</summary>
    public const string ProductsCreate = "inventory.products.create";

    /// <summary>Update products and change their lifecycle.</summary>
    public const string ProductsUpdate = "inventory.products.update";

    /// <summary>Read warehouses.</summary>
    public const string WarehousesRead = "inventory.warehouses.read";

    /// <summary>Create warehouses.</summary>
    public const string WarehousesCreate = "inventory.warehouses.create";

    /// <summary>Update warehouses and change their lifecycle.</summary>
    public const string WarehousesUpdate = "inventory.warehouses.update";

    /// <summary>Read stock positions.</summary>
    public const string StockRead = "inventory.stock.read";

    /// <summary>Change stock and reservations.</summary>
    public const string StockUpdate = "inventory.stock.update";
}

/// <summary>
/// The Inventory module's permission catalog. It is registered as a platform catalog source so the
/// Identity system roles remain correct as the module grows (ADR-018).
/// </summary>
public sealed class InventoryPermissionCatalog : IPermissionCatalogSource
{
    /// <inheritdoc />
    public IReadOnlyCollection<PermissionDefinition> Permissions { get; } =
    [
        new(InventoryPermissions.ProductsRead, "Read products.", PermissionScope.Tenant),
        new(InventoryPermissions.ProductsCreate, "Create products.", PermissionScope.Tenant),
        new(InventoryPermissions.ProductsUpdate, "Update products and change their lifecycle.", PermissionScope.Tenant),
        new(InventoryPermissions.WarehousesRead, "Read warehouses.", PermissionScope.Tenant),
        new(InventoryPermissions.WarehousesCreate, "Create warehouses.", PermissionScope.Tenant),
        new(InventoryPermissions.WarehousesUpdate, "Update warehouses and change their lifecycle.", PermissionScope.Tenant),
        new(InventoryPermissions.StockRead, "Read inventory stock positions.", PermissionScope.Tenant),
        new(InventoryPermissions.StockUpdate, "Receive, reserve, release and adjust stock.", PermissionScope.Tenant),
    ];
}
