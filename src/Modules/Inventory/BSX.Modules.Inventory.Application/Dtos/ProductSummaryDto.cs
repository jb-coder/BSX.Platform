namespace BSX.Modules.Inventory.Application.Dtos;

/// <summary>A lightweight product projection for list screens.</summary>
public sealed record ProductSummaryDto(
    Guid Id,
    string Sku,
    string Name,
    string Status);
