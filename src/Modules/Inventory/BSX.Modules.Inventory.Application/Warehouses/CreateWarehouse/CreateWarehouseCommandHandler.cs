using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Inventory.Application.Abstractions;
using BSX.Modules.Inventory.Domain.ValueObjects;
using BSX.Modules.Inventory.Domain.Warehouses;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Inventory.Application.Warehouses.CreateWarehouse;

/// <summary>Handles <see cref="CreateWarehouseCommand"/>.</summary>
public sealed class CreateWarehouseCommandHandler : ICommandHandler<CreateWarehouseCommand, Guid>
{
    private readonly IWarehouseRepository _warehouses;
    private readonly IUnitOfWork _unitOfWork;
    private readonly InventoryTenantResolver _tenantResolver;

    /// <summary>Initializes a new instance of the <see cref="CreateWarehouseCommandHandler"/> class.</summary>
    public CreateWarehouseCommandHandler(
        IWarehouseRepository warehouses,
        IUnitOfWork unitOfWork,
        InventoryTenantResolver tenantResolver)
    {
        _warehouses = warehouses;
        _unitOfWork = unitOfWork;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> HandleAsync(
        CreateWarehouseCommand request,
        CancellationToken cancellationToken = default)
    {
        Result<TenantId> tenant = _tenantResolver.Resolve();
        if (tenant.IsFailure)
        {
            return Result.Failure<Guid>(tenant.Error);
        }

        Result<WarehouseName> name = WarehouseName.Create(request.Name);
        if (name.IsFailure)
        {
            return Result.Failure<Guid>(name.Error);
        }

        Result<Warehouse> created = Warehouse.Create(WarehouseId.New(), tenant.Value, name.Value);
        if (created.IsFailure)
        {
            return Result.Failure<Guid>(created.Error);
        }

        await _warehouses.AddAsync(created.Value, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(created.Value.Id.Value);
    }
}
