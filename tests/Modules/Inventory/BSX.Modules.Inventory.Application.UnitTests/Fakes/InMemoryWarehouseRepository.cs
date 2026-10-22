using BSX.Modules.Inventory.Domain.ValueObjects;
using BSX.Modules.Inventory.Domain.Warehouses;

namespace BSX.Modules.Inventory.Application.UnitTests.Fakes;

internal sealed class InMemoryWarehouseRepository : IWarehouseRepository
{
    private readonly List<Warehouse> _warehouses = [];

    public IReadOnlyCollection<Warehouse> Warehouses => _warehouses;

    public TenantId? LastRequestedTenant { get; private set; }

    public void Seed(params Warehouse[] warehouses) => _warehouses.AddRange(warehouses);

    public Task<Warehouse?> GetByIdAsync(
        TenantId tenantId,
        WarehouseId id,
        CancellationToken cancellationToken = default)
    {
        LastRequestedTenant = tenantId;
        return Task.FromResult(_warehouses.Find(warehouse => warehouse.Id == id && warehouse.TenantId == tenantId));
    }

    public Task AddAsync(Warehouse warehouse, CancellationToken cancellationToken = default)
    {
        _warehouses.Add(warehouse);
        return Task.CompletedTask;
    }
}
