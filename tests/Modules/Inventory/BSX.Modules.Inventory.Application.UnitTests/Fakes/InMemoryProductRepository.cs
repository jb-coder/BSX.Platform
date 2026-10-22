using BSX.Modules.Inventory.Domain.Products;
using BSX.Modules.Inventory.Domain.ValueObjects;

namespace BSX.Modules.Inventory.Application.UnitTests.Fakes;

internal sealed class InMemoryProductRepository : IProductRepository
{
    private readonly List<Product> _products = [];

    public IReadOnlyCollection<Product> Products => _products;

    public TenantId? LastRequestedTenant { get; private set; }

    public Sku? LastCheckedSku { get; private set; }

    public void Seed(params Product[] products) => _products.AddRange(products);

    public Task<Product?> GetByIdAsync(
        TenantId tenantId,
        ProductId id,
        CancellationToken cancellationToken = default)
    {
        LastRequestedTenant = tenantId;
        return Task.FromResult(_products.Find(product => product.Id == id && product.TenantId == tenantId));
    }

    public Task<bool> ExistsBySkuAsync(
        TenantId tenantId,
        Sku sku,
        CancellationToken cancellationToken = default)
    {
        LastRequestedTenant = tenantId;
        LastCheckedSku = sku;
        return Task.FromResult(_products.Exists(product => product.TenantId == tenantId && product.Sku == sku));
    }

    public Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        _products.Add(product);
        return Task.CompletedTask;
    }
}
