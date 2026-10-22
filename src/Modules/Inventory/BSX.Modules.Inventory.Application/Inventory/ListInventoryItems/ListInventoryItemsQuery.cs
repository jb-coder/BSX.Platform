using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Inventory.Application.Common;
using BSX.Modules.Inventory.Application.Dtos;

namespace BSX.Modules.Inventory.Application.Inventory.ListInventoryItems;

/// <summary>Lists stock-position summaries within the active tenant.</summary>
/// <param name="Page">The one-based page number.</param>
/// <param name="Size">The page size.</param>
/// <param name="Search">An optional product SKU/name or warehouse name search term.</param>
[RequirePermission(InventoryPermissions.StockRead)]
public sealed record ListInventoryItemsQuery(int Page, int Size, string? Search)
    : IQuery<PagedResult<InventoryItemSummaryDto>>;
