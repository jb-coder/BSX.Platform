using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Inventory.Application.Abstractions;
using BSX.Modules.Inventory.Application.Dtos;
using BSX.Modules.Inventory.Application.Errors;
using BSX.Modules.Inventory.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Inventory.Application.Inventory.GetInventoryItemById;

/// <summary>
/// Handles <see cref="GetInventoryItemByIdQuery"/>. Projects through the read model; loads no
/// aggregate.
/// </summary>
public sealed class GetInventoryItemByIdQueryHandler
    : IQueryHandler<GetInventoryItemByIdQuery, InventoryItemDto>
{
    private readonly IInventoryReadRepository _inventory;
    private readonly InventoryTenantResolver _tenantResolver;

    /// <summary>Initializes a new instance of the <see cref="GetInventoryItemByIdQueryHandler"/> class.</summary>
    public GetInventoryItemByIdQueryHandler(
        IInventoryReadRepository inventory,
        InventoryTenantResolver tenantResolver)
    {
        _inventory = inventory;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc />
    public async Task<Result<InventoryItemDto>> HandleAsync(
        GetInventoryItemByIdQuery request,
        CancellationToken cancellationToken = default)
    {
        if (request.InventoryItemId == Guid.Empty)
        {
            return Result.Failure<InventoryItemDto>(InventoryApplicationErrors.InvalidInventoryItemId);
        }

        Result<TenantId> tenant = _tenantResolver.Resolve();
        if (tenant.IsFailure)
        {
            return Result.Failure<InventoryItemDto>(tenant.Error);
        }

        InventoryItemDto? item = await _inventory
            .GetByIdAsync(tenant.Value.Value, request.InventoryItemId, cancellationToken)
            .ConfigureAwait(false);

        return item is null
            ? Result.Failure<InventoryItemDto>(InventoryApplicationErrors.InventoryItemNotFound)
            : Result.Success(item);
    }
}
