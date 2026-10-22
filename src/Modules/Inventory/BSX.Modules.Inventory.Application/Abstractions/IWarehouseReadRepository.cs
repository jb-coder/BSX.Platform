using BSX.Modules.Inventory.Application.Dtos;

namespace BSX.Modules.Inventory.Application.Abstractions;

/// <summary>Read-model access for warehouses (CQRS read side). Implemented in Infrastructure.</summary>
public interface IWarehouseReadRepository
{
    /// <summary>Gets a warehouse projection within a tenant.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="warehouseId">The warehouse identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<WarehouseDto?> GetByIdAsync(Guid tenantId, Guid warehouseId, CancellationToken cancellationToken = default);

    /// <summary>Lists warehouse summaries within a tenant, with paging and an optional search term.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="page">The one-based page number.</param>
    /// <param name="size">The page size.</param>
    /// <param name="search">An optional name search term.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<PagedResult<WarehouseSummaryDto>> ListAsync(
        Guid tenantId,
        int page,
        int size,
        string? search,
        CancellationToken cancellationToken = default);
}
