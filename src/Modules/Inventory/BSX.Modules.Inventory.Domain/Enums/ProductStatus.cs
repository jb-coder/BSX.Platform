namespace BSX.Modules.Inventory.Domain.Enums;

/// <summary>The lifecycle status of a product.</summary>
public enum ProductStatus
{
    /// <summary>The product is active and can participate in stock operations.</summary>
    Active = 0,

    /// <summary>The product is deactivated and must not receive new stock operations.</summary>
    Inactive = 1,
}
