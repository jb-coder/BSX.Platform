namespace BSX.Modules.Inventory.Domain.ValueObjects;

/// <summary>Strongly typed identifier for an inventory item (stock of a product in a warehouse).</summary>
public sealed class InventoryItemId : InventoryId
{
    private InventoryItemId(Guid value)
        : base(value)
    {
    }

    /// <summary>Creates a new identifier.</summary>
    public static InventoryItemId New() => new(Guid.NewGuid());

    /// <summary>Creates an identifier from an existing value.</summary>
    /// <param name="value">The identifier value.</param>
    public static InventoryItemId From(Guid value) => new(value);
}
