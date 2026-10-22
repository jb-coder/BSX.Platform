using BSX.Modules.Inventory.Domain.InventoryItems;
using BSX.Modules.Inventory.Domain.ValueObjects;

namespace BSX.Modules.Inventory.Application.UnitTests.Fakes;

internal sealed class InMemoryInventoryItemRepository : IInventoryItemRepository
{
    private readonly List<InventoryItem> _items = [];

    public IReadOnlyCollection<InventoryItem> Items => _items;

    public TenantId? LastRequestedTenant { get; private set; }

    public void Seed(params InventoryItem[] items) => _items.AddRange(items);

    public Task<InventoryItem?> GetByIdAsync(
        TenantId tenantId,
        InventoryItemId id,
        CancellationToken cancellationToken = default)
    {
        LastRequestedTenant = tenantId;
        return Task.FromResult(_items.Find(item => item.Id == id && item.TenantId == tenantId));
    }

    public Task<InventoryItem?> GetByProductAndWarehouseAsync(
        TenantId tenantId,
        ProductId productId,
        WarehouseId warehouseId,
        CancellationToken cancellationToken = default)
    {
        LastRequestedTenant = tenantId;
        return Task.FromResult(_items.Find(item =>
            item.TenantId == tenantId
            && item.ProductId == productId
            && item.WarehouseId == warehouseId));
    }

    public Task<bool> ExistsByProductAndWarehouseAsync(
        TenantId tenantId,
        ProductId productId,
        WarehouseId warehouseId,
        CancellationToken cancellationToken = default)
    {
        LastRequestedTenant = tenantId;
        return Task.FromResult(_items.Exists(item =>
            item.TenantId == tenantId
            && item.ProductId == productId
            && item.WarehouseId == warehouseId));
    }

    public Task AddAsync(InventoryItem item, CancellationToken cancellationToken = default)
    {
        _items.Add(item);
        return Task.CompletedTask;
    }
}
