using BSX.Modules.Inventory.Application.Abstractions;
using BSX.Modules.Inventory.Application.Dtos;

namespace BSX.Modules.Inventory.Application.UnitTests.Fakes;

internal sealed class FakeProductReadRepository : IProductReadRepository
{
    public ProductDto? Product { get; set; }

    public PagedResult<ProductSummaryDto> Page { get; set; } = new([], 1, 20, 0);

    public Guid? LastTenantId { get; private set; }

    public int LastPage { get; private set; }

    public int LastSize { get; private set; }

    public string? LastSearch { get; private set; }

    public Task<ProductDto?> GetByIdAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        LastTenantId = tenantId;
        return Task.FromResult(Product is not null && Product.Id == productId ? Product : null);
    }

    public Task<PagedResult<ProductSummaryDto>> ListAsync(
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
