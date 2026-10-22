namespace BSX.Modules.Inventory.Application.Dtos;

/// <summary>A lightweight warehouse projection for list screens.</summary>
public sealed record WarehouseSummaryDto(
    Guid Id,
    string Name,
    string Status);
