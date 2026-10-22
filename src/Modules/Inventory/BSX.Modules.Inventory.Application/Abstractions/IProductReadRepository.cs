using BSX.Modules.Inventory.Application.Dtos;

namespace BSX.Modules.Inventory.Application.Abstractions;

/// <summary>Read-model access for products (CQRS read side). Implemented in Infrastructure.</summary>
public interface IProductReadRepository
{
    /// <summary>Gets a product projection within a tenant.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="productId">The product identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<ProductDto?> GetByIdAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken = default);

    /// <summary>Lists product summaries within a tenant, with paging and an optional search term.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="page">The one-based page number.</param>
    /// <param name="size">The page size.</param>
    /// <param name="search">An optional SKU or name search term.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<PagedResult<ProductSummaryDto>> ListAsync(
        Guid tenantId,
        int page,
        int size,
        string? search,
        CancellationToken cancellationToken = default);
}
