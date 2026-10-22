namespace BSX.Modules.Inventory.Application.Dtos;

/// <summary>A lightweight stock-position projection for list screens.</summary>
public sealed record InventoryItemSummaryDto(
    Guid Id,
    Guid ProductId,
    Guid WarehouseId,
    int OnHandQuantity,
    int ReservedQuantity,
    int AvailableQuantity);
