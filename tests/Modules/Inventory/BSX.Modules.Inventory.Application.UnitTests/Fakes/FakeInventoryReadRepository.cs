using BSX.Modules.Inventory.Application.Abstractions;
using BSX.Modules.Inventory.Application.Dtos;

namespace BSX.Modules.Inventory.Application.UnitTests.Fakes;

internal sealed class FakeInventoryReadRepository : IInventoryReadRepository
{
    public InventoryItemDto? Item { get; set; }

    public PagedResult<InventoryItemSummaryDto> Page { get; set; } = new([], 1, 20, 0);

    public Guid? LastTenantId { get; private set; }

    public int LastPage { get; private set; }

    public int LastSize { get; private set; }

    public string? LastSearch { get; private set; }

    public Task<InventoryItemDto?> GetByIdAsync(
        Guid tenantId,
        Guid inventoryItemId,
        CancellationToken cancellationToken = default)
    {
        LastTenantId = tenantId;
        return Task.FromResult(Item is not null && Item.Id == inventoryItemId ? Item : null);
    }

    public Task<PagedResult<InventoryItemSummaryDto>> ListAsync(
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
