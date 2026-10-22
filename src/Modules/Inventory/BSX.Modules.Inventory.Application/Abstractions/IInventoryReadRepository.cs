using BSX.Modules.Inventory.Application.Dtos;

namespace BSX.Modules.Inventory.Application.Abstractions;

/// <summary>Read-model access for stock positions (CQRS read side). Implemented in Infrastructure.</summary>
public interface IInventoryReadRepository
{
    /// <summary>Gets a stock-position projection within a tenant.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="inventoryItemId">The inventory item identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<InventoryItemDto?> GetByIdAsync(
        Guid tenantId,
        Guid inventoryItemId,
        CancellationToken cancellationToken = default);

    /// <summary>Lists stock-position summaries within a tenant, with paging and an optional search term.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="page">The one-based page number.</param>
    /// <param name="size">The page size.</param>
    /// <param name="search">An optional product SKU/name or warehouse name search term.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<PagedResult<InventoryItemSummaryDto>> ListAsync(
        Guid tenantId,
        int page,
        int size,
        string? search,
        CancellationToken cancellationToken = default);
}
