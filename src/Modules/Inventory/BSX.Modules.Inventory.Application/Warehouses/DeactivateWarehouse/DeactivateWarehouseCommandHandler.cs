using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Inventory.Application.Abstractions;
using BSX.Modules.Inventory.Application.Errors;
using BSX.Modules.Inventory.Domain.ValueObjects;
using BSX.Modules.Inventory.Domain.Warehouses;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Inventory.Application.Warehouses.DeactivateWarehouse;

/// <summary>Handles <see cref="DeactivateWarehouseCommand"/>.</summary>
public sealed class DeactivateWarehouseCommandHandler : ICommandHandler<DeactivateWarehouseCommand>
{
    private readonly IWarehouseRepository _warehouses;
    private readonly IUnitOfWork _unitOfWork;
    private readonly InventoryTenantResolver _tenantResolver;

    /// <summary>Initializes a new instance of the <see cref="DeactivateWarehouseCommandHandler"/> class.</summary>
    public DeactivateWarehouseCommandHandler(
        IWarehouseRepository warehouses,
        IUnitOfWork unitOfWork,
        InventoryTenantResolver tenantResolver)
    {
        _warehouses = warehouses;
        _unitOfWork = unitOfWork;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc />
    public async Task<Result> HandleAsync(
        DeactivateWarehouseCommand request,
        CancellationToken cancellationToken = default)
    {
        if (request.WarehouseId == Guid.Empty)
        {
            return Result.Failure(InventoryApplicationErrors.InvalidWarehouseId);
        }

        Result<TenantId> tenant = _tenantResolver.Resolve();
        if (tenant.IsFailure)
        {
            return Result.Failure(tenant.Error);
        }

        Warehouse? warehouse = await _warehouses
            .GetByIdAsync(tenant.Value, WarehouseId.From(request.WarehouseId), cancellationToken)
            .ConfigureAwait(false);

        if (warehouse is null)
        {
            return Result.Failure(InventoryApplicationErrors.WarehouseNotFound);
        }

        Result deactivated = warehouse.Deactivate();
        if (deactivated.IsFailure)
        {
            return deactivated;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
