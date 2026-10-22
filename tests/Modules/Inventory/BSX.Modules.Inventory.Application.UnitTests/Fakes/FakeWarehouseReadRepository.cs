using BSX.Modules.Inventory.Application.Abstractions;
using BSX.Modules.Inventory.Application.Dtos;

namespace BSX.Modules.Inventory.Application.UnitTests.Fakes;

internal sealed class FakeWarehouseReadRepository : IWarehouseReadRepository
{
    public WarehouseDto? Warehouse { get; set; }

    public PagedResult<WarehouseSummaryDto> Page { get; set; } = new([], 1, 20, 0);

    public Guid? LastTenantId { get; private set; }

    public int LastPage { get; private set; }

    public int LastSize { get; private set; }

    public string? LastSearch { get; private set; }

    public Task<WarehouseDto?> GetByIdAsync(
        Guid tenantId,
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        LastTenantId = tenantId;
        return Task.FromResult(Warehouse is not null && Warehouse.Id == warehouseId ? Warehouse : null);
    }

    public Task<PagedResult<WarehouseSummaryDto>> ListAsync(
        Guid tenantId,
        int page,
        int size,
        string? search,
        CancellationToken cancellationToken = default)
    {
        LastTenantId = tenantId;
        LastPage = page;
        LastSize = size;
        LastSearch = search;
        return Task.FromResult(Page);
    }
}
