using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Inventory.Application.Common;
using BSX.Modules.Inventory.Application.Dtos;

namespace BSX.Modules.Inventory.Application.Warehouses.ListWarehouses;

/// <summary>Lists warehouse summaries within the active tenant.</summary>
/// <param name="Page">The one-based page number.</param>
/// <param name="Size">The page size.</param>
/// <param name="Search">An optional name search term.</param>
[RequirePermission(InventoryPermissions.WarehousesRead)]
public sealed record ListWarehousesQuery(int Page, int Size, string? Search) : IQuery<PagedResult<WarehouseSummaryDto>>;
