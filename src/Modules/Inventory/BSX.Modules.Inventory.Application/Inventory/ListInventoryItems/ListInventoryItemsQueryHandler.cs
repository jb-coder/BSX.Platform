using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Inventory.Application.Abstractions;
using BSX.Modules.Inventory.Application.Dtos;
using BSX.Modules.Inventory.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Inventory.Application.Inventory.ListInventoryItems;

/// <summary>
/// Handles <see cref="ListInventoryItemsQuery"/>. Projects through the read model; loads no
/// aggregate.
/// </summary>
public sealed class ListInventoryItemsQueryHandler
    : IQueryHandler<ListInventoryItemsQuery, PagedResult<InventoryItemSummaryDto>>
{
    private const int MaxSize = 100;
    private const int DefaultSize = 20;

    private readonly IInventoryReadRepository _inventory;
    private readonly InventoryTenantResolver _tenantResolver;

    /// <summary>Initializes a new instance of the <see cref="ListInventoryItemsQueryHandler"/> class.</summary>
    public ListInventoryItemsQueryHandler(
        IInventoryReadRepository inventory,
        InventoryTenantResolver tenantResolver)
    {
        _inventory = inventory;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<InventoryItemSummaryDto>>> HandleAsync(
        ListInventoryItemsQuery request,
        CancellationToken cancellationToken = default)
    {
        Result<TenantId> tenant = _tenantResolver.Resolve();
        if (tenant.IsFailure)
        {
            return Result.Failure<PagedResult<InventoryItemSummaryDto>>(tenant.Error);
        }

        int page = request.Page < 1 ? 1 : request.Page;
        int size = request.Size is < 1 or > MaxSize ? DefaultSize : request.Size;

        PagedResult<InventoryItemSummaryDto> result = await _inventory
            .ListAsync(tenant.Value.Value, page, size, request.Search, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(result);
    }
}
