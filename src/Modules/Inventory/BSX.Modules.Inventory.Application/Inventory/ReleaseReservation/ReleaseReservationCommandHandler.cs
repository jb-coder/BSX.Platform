using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Inventory.Application.Abstractions;
using BSX.Modules.Inventory.Application.Common;
using BSX.Modules.Inventory.Application.Errors;
using BSX.Modules.Inventory.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Inventory.Application.Inventory.ReleaseReservation;

/// <summary>Handles <see cref="ReleaseReservationCommand"/>.</summary>
public sealed class ReleaseReservationCommandHandler : ICommandHandler<ReleaseReservationCommand>
{
    private readonly InventoryStockPolicy _policy;
    private readonly IUnitOfWork _unitOfWork;
    private readonly InventoryTenantResolver _tenantResolver;

    /// <summary>Initializes a new instance of the <see cref="ReleaseReservationCommandHandler"/> class.</summary>
    public ReleaseReservationCommandHandler(
        InventoryStockPolicy policy,
        IUnitOfWork unitOfWork,
        InventoryTenantResolver tenantResolver)
    {
        _policy = policy;
        _unitOfWork = unitOfWork;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc />
    public async Task<Result> HandleAsync(
        ReleaseReservationCommand request,
        CancellationToken cancellationToken = default)
    {
        if (request.ProductId == Guid.Empty)
        {
            return Result.Failure(InventoryApplicationErrors.InvalidProductId);
        }

        if (request.WarehouseId == Guid.Empty)
        {
            return Result.Failure(InventoryApplicationErrors.InvalidWarehouseId);
        }

        Result<TenantId> tenant = _tenantResolver.Resolve();
        if (tenant.IsFailure)
        {
            return Result.Failure(tenant.Error);
        }

        Result<Quantity> quantity = Quantity.Create(request.Quantity);
        if (quantity.IsFailure)
        {
            return Result.Failure(quantity.Error);
        }

        Result<InventoryStockResolution> resolution = await _policy
            .ResolveAsync(
                tenant.Value,
                ProductId.From(request.ProductId),
                WarehouseId.From(request.WarehouseId),
                createIfMissing: false,
                cancellationToken)
            .ConfigureAwait(false);

        if (resolution.IsFailure)
        {
            return Result.Failure(resolution.Error);
        }

        Result released = resolution.Value.Item.Release(quantity.Value);
        if (released.IsFailure)
        {
            return released;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}

