using BSX.Modules.Inventory.Domain.ValueObjects;

namespace BSX.Modules.Inventory.Domain.Products;

/// <summary>Persistence contract for the <see cref="Product"/> aggregate (aggregate-specific).</summary>
public interface IProductRepository
{
    /// <summary>Gets a product by identifier within a tenant.</summary>
    /// <param name="tenantId">The tenant identifier (read isolation).</param>
    /// <param name="id">The product identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<Product?> GetByIdAsync(TenantId tenantId, ProductId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a SKU exists within a tenant. SKU uniqueness is an Infrastructure
    /// concern; the domain never queries this itself.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="sku">The SKU.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<bool> ExistsBySkuAsync(TenantId tenantId, Sku sku, CancellationToken cancellationToken = default);

    /// <summary>Adds a new product.</summary>
    /// <param name="product">The product.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
}
