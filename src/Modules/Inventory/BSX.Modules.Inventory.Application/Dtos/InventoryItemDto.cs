namespace BSX.Modules.Inventory.Application.Dtos;

/// <summary>A stock-position detail projection with the derived free quantity.</summary>
public sealed record InventoryItemDto(
    Guid Id,
    Guid TenantId,
    Guid ProductId,
    Guid WarehouseId,
    int OnHandQuantity,
    int ReservedQuantity,
    int AvailableQuantity);
