using BSX.SharedKernel.Results;

namespace BSX.Modules.Inventory.Application.Errors;

/// <summary>Stable error catalog for the Inventory application layer.</summary>
public static class InventoryApplicationErrors
{
    /// <summary>The request has no active tenant scope.</summary>
    public static Error TenantRequired => Error.Forbidden(
        "INVENTORY.Tenant.Required",
        "An active tenant is required.");

    /// <summary>The product was not found in the tenant.</summary>
    public static Error ProductNotFound => Error.NotFound(
        "INVENTORY.Product.NotFound",
        "The product was not found.");

    /// <summary>The product is inactive and cannot participate in stock operations.</summary>
    public static Error ProductInactive => Error.Conflict(
        "INVENTORY.Product.Inactive",
        "The product is inactive.");

    /// <summary>The SKU is already used by another product in the tenant.</summary>
    public static Error ProductSkuAlreadyExists => Error.Conflict(
        "INVENTORY.Product.SkuAlreadyExists",
        "A product with the same SKU already exists in the active tenant.");

    /// <summary>The warehouse was not found in the tenant.</summary>
    public static Error WarehouseNotFound => Error.NotFound(
        "INVENTORY.Warehouse.NotFound",
        "The warehouse was not found.");

    /// <summary>The warehouse is inactive and cannot participate in stock operations.</summary>
    public static Error WarehouseInactive => Error.Conflict(
        "INVENTORY.Warehouse.Inactive",
        "The warehouse is inactive.");

    /// <summary>The inventory item was not found in the tenant.</summary>
    public static Error InventoryItemNotFound => Error.NotFound(
        "INVENTORY.InventoryItem.NotFound",
        "The inventory item was not found.");

    /// <summary>The product identifier is invalid.</summary>
    public static Error InvalidProductId => Error.Validation(
        "INVENTORY.Product.InvalidId",
        "The product identifier is invalid.");

    /// <summary>The warehouse identifier is invalid.</summary>
    public static Error InvalidWarehouseId => Error.Validation(
        "INVENTORY.Warehouse.InvalidId",
        "The warehouse identifier is invalid.");

    /// <summary>The inventory item identifier is invalid.</summary>
    public static Error InvalidInventoryItemId => Error.Validation(
        "INVENTORY.InventoryItem.InvalidId",
        "The inventory item identifier is invalid.");
}
