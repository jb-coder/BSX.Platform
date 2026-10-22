namespace BSX.Modules.Inventory.Domain.ValueObjects;

/// <summary>Strongly typed identifier for a product.</summary>
public sealed class ProductId : InventoryId
{
    private ProductId(Guid value)
        : base(value)
    {
    }

    /// <summary>Creates a new identifier.</summary>
    public static ProductId New() => new(Guid.NewGuid());

    /// <summary>Creates an identifier from an existing value.</summary>
    /// <param name="value">The identifier value.</param>
    public static ProductId From(Guid value) => new(value);
}
