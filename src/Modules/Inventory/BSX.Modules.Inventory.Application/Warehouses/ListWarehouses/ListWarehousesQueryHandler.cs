using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Inventory.Application.Abstractions;
using BSX.Modules.Inventory.Application.Dtos;
using BSX.Modules.Inventory.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Inventory.Application.Warehouses.ListWarehouses;

/// <summary>Handles <see cref="ListWarehousesQuery"/>. Projects through the read model; loads no aggregate.</summary>
public sealed class ListWarehousesQueryHandler
    : IQueryHandler<ListWarehousesQuery, PagedResult<WarehouseSummaryDto>>
{
    private const int MaxSize = 100;
    private const int DefaultSize = 20;

    private readonly IWarehouseReadRepository _warehouses;
    private readonly InventoryTenantResolver _tenantResolver;

    /// <summary>Initializes a new instance of the <see cref="ListWarehousesQueryHandler"/> class.</summary>
    public ListWarehousesQueryHandler(
        IWarehouseReadRepository warehouses,
        InventoryTenantResolver tenantResolver)
    {
        _warehouses = warehouses;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<WarehouseSummaryDto>>> HandleAsync(
        ListWarehousesQuery request,
        CancellationToken cancellationToken = default)
    {
        Result<TenantId> tenant = _tenantResolver.Resolve();
        if (tenant.IsFailure)
        {
            return Result.Failure<PagedResult<WarehouseSummaryDto>>(tenant.Error);
        }

        int page = request.Page < 1 ? 1 : request.Page;
        int size = request.Size is < 1 or > MaxSize ? DefaultSize : request.Size;

        PagedResult<WarehouseSummaryDto> result = await _warehouses
            .ListAsync(tenant.Value.Value, page, size, request.Search, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(result);
    }
}
