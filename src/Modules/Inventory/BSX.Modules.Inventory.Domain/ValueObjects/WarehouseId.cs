namespace BSX.Modules.Inventory.Domain.ValueObjects;

/// <summary>Strongly typed identifier for a warehouse.</summary>
public sealed class WarehouseId : InventoryId
{
    private WarehouseId(Guid value)
        : base(value)
    {
    }

    /// <summary>Creates a new identifier.</summary>
    public static WarehouseId New() => new(Guid.NewGuid());

    /// <summary>Creates an identifier from an existing value.</summary>
    /// <param name="value">The identifier value.</param>
    public static WarehouseId From(Guid value) => new(value);
}
