using BSX.SharedKernel.Results;

namespace BSX.Modules.Inventory.Domain.Errors;

/// <summary>
/// Stable, namespaced error catalog for the Inventory domain (ADR-011).
/// Codes never change once published.
/// </summary>
public static class InventoryErrors
{
    /// <summary>The SKU is invalid.</summary>
    public static Error SkuInvalid => Error.Validation(
        "INVENTORY.Sku.Invalid",
        "The SKU must be non-empty, at most 64 characters and contain only letters, digits, '-', '_' or '.'.");

    /// <summary>The product name is invalid.</summary>
    public static Error ProductNameInvalid => Error.Validation(
        "INVENTORY.ProductName.Invalid",
        "The product name is invalid.");

    /// <summary>The warehouse name is invalid.</summary>
    public static Error WarehouseNameInvalid => Error.Validation(
        "INVENTORY.WarehouseName.Invalid",
        "The warehouse name is invalid.");

    /// <summary>The quantity is invalid.</summary>
    public static Error QuantityInvalid => Error.Validation(
        "INVENTORY.Quantity.Invalid",
        "The quantity must be a non-negative whole number within the supported range.");

    /// <summary>The product is already active.</summary>
    public static Error ProductAlreadyActive => Error.Conflict(
        "INVENTORY.Product.AlreadyActive",
        "The product is already active.");

    /// <summary>The product is already inactive.</summary>
    public static Error ProductAlreadyInactive => Error.Conflict(
        "INVENTORY.Product.AlreadyInactive",
        "The product is already inactive.");

    /// <summary>The warehouse is already active.</summary>
    public static Error WarehouseAlreadyActive => Error.Conflict(
        "INVENTORY.Warehouse.AlreadyActive",
        "The warehouse is already active.");

    /// <summary>The warehouse is already inactive.</summary>
    public static Error WarehouseAlreadyInactive => Error.Conflict(
        "INVENTORY.Warehouse.AlreadyInactive",
        "The warehouse is already inactive.");

    /// <summary>A stock movement must use a positive quantity.</summary>
    public static Error StockQuantityNotPositive => Error.Validation(
        "INVENTORY.Stock.QuantityNotPositive",
        "A stock movement must use a positive quantity.");

    /// <summary>The stock movement would exceed the supported quantity range.</summary>
    public static Error StockQuantityOverflow => Error.Validation(
        "INVENTORY.Stock.QuantityOverflow",
        "The stock movement would exceed the supported quantity range.");

    /// <summary>There is not enough available stock to reserve the requested quantity.</summary>
    public static Error InsufficientAvailableStock => Error.Conflict(
        "INVENTORY.Stock.InsufficientAvailable",
        "The requested quantity exceeds the available stock.");

    /// <summary>There is not enough reserved stock to release the requested quantity.</summary>
    public static Error InsufficientReservedStock => Error.Conflict(
        "INVENTORY.Stock.InsufficientReserved",
        "The requested quantity exceeds the reserved stock.");

    /// <summary>An adjustment cannot change the stock because the delta is zero.</summary>
    public static Error StockAdjustmentIsZero => Error.Validation(
        "INVENTORY.Stock.AdjustmentIsZero",
        "The stock adjustment must not be zero.");

    /// <summary>An adjustment would make the on-hand quantity negative.</summary>
    public static Error StockAdjustmentWouldBeNegative => Error.Conflict(
        "INVENTORY.Stock.AdjustmentWouldBeNegative",
        "The stock adjustment would make the on-hand quantity negative.");

    /// <summary>An adjustment would make the on-hand quantity lower than the reserved quantity.</summary>
    public static Error StockAdjustmentBelowReserved => Error.Conflict(
        "INVENTORY.Stock.AdjustmentBelowReserved",
        "The stock adjustment would make the on-hand quantity lower than the reserved quantity.");
}
