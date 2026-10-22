using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Inventory.Application.Abstractions;
using BSX.Modules.Inventory.Application.Dtos;
using BSX.Modules.Inventory.Application.Errors;
using BSX.Modules.Inventory.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Inventory.Application.Warehouses.GetWarehouseById;

/// <summary>Handles <see cref="GetWarehouseByIdQuery"/>. Projects through the read model; loads no aggregate.</summary>
public sealed class GetWarehouseByIdQueryHandler : IQueryHandler<GetWarehouseByIdQuery, WarehouseDto>
{
    private readonly IWarehouseReadRepository _warehouses;
    private readonly InventoryTenantResolver _tenantResolver;

    /// <summary>Initializes a new instance of the <see cref="GetWarehouseByIdQueryHandler"/> class.</summary>
    public GetWarehouseByIdQueryHandler(
        IWarehouseReadRepository warehouses,
        InventoryTenantResolver tenantResolver)
    {
        _warehouses = warehouses;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc />
    public async Task<Result<WarehouseDto>> HandleAsync(
        GetWarehouseByIdQuery request,
        CancellationToken cancellationToken = default)
    {
        if (request.WarehouseId == Guid.Empty)
        {
            return Result.Failure<WarehouseDto>(InventoryApplicationErrors.InvalidWarehouseId);
        }

        Result<TenantId> tenant = _tenantResolver.Resolve();
        if (tenant.IsFailure)
        {
            return Result.Failure<WarehouseDto>(tenant.Error);
        }

        WarehouseDto? warehouse = await _warehouses
            .GetByIdAsync(tenant.Value.Value, request.WarehouseId, cancellationToken)
            .ConfigureAwait(false);

        return warehouse is null
            ? Result.Failure<WarehouseDto>(InventoryApplicationErrors.WarehouseNotFound)
            : Result.Success(warehouse);
    }
}
