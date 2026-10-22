namespace BSX.Modules.Inventory.Domain.ValueObjects;

/// <summary>
/// Strongly typed tenant identifier. Every Inventory aggregate is tenant-owned (ADR-013); the
/// identifier is carried by the aggregate so Infrastructure can enforce isolation.
/// </summary>
public sealed class TenantId : InventoryId
{
    private TenantId(Guid value)
        : base(value)
    {
    }

    /// <summary>Creates a new identifier.</summary>
    public static TenantId New() => new(Guid.NewGuid());

    /// <summary>Creates an identifier from an existing value.</summary>
    /// <param name="value">The identifier value.</param>
    public static TenantId From(Guid value) => new(value);
}
