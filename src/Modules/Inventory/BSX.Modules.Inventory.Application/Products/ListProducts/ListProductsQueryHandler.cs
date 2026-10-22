using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Inventory.Application.Abstractions;
using BSX.Modules.Inventory.Application.Dtos;
using BSX.Modules.Inventory.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Inventory.Application.Products.ListProducts;

/// <summary>Handles <see cref="ListProductsQuery"/>. Projects through the read model; loads no aggregate.</summary>
public sealed class ListProductsQueryHandler : IQueryHandler<ListProductsQuery, PagedResult<ProductSummaryDto>>
{
    private const int MaxSize = 100;
    private const int DefaultSize = 20;

    private readonly IProductReadRepository _products;
    private readonly InventoryTenantResolver _tenantResolver;

    /// <summary>Initializes a new instance of the <see cref="ListProductsQueryHandler"/> class.</summary>
    public ListProductsQueryHandler(IProductReadRepository products, InventoryTenantResolver tenantResolver)
    {
        _products = products;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<ProductSummaryDto>>> HandleAsync(
        ListProductsQuery request,
        CancellationToken cancellationToken = default)
    {
        Result<TenantId> tenant = _tenantResolver.Resolve();
        if (tenant.IsFailure)
        {
            return Result.Failure<PagedResult<ProductSummaryDto>>(tenant.Error);
        }

        int page = request.Page < 1 ? 1 : request.Page;
        int size = request.Size is < 1 or > MaxSize ? DefaultSize : request.Size;

        PagedResult<ProductSummaryDto> result = await _products
            .ListAsync(tenant.Value.Value, page, size, request.Search, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(result);
    }
}
