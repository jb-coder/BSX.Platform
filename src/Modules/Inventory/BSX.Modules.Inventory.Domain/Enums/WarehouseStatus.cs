namespace BSX.Modules.Inventory.Domain.Enums;

/// <summary>The operational status of a warehouse.</summary>
public enum WarehouseStatus
{
    /// <summary>The warehouse is active and can participate in stock operations.</summary>
    Active = 0,

    /// <summary>The warehouse is deactivated and must not receive new stock operations.</summary>
    Inactive = 1,
}
