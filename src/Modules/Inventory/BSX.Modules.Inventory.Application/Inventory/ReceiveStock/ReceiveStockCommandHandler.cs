using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Inventory.Application.Abstractions;
using BSX.Modules.Inventory.Application.Common;
using BSX.Modules.Inventory.Application.Errors;
using BSX.Modules.Inventory.Domain.InventoryItems;
using BSX.Modules.Inventory.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Inventory.Application.Inventory.ReceiveStock;

/// <summary>Handles <see cref="ReceiveStockCommand"/>.</summary>
public sealed class ReceiveStockCommandHandler : ICommandHandler<ReceiveStockCommand, Guid>
{
    private readonly InventoryStockPolicy _policy;
    private readonly IInventoryItemRepository _inventoryItems;
    private readonly IUnitOfWork _unitOfWork;
    private readonly InventoryTenantResolver _tenantResolver;

    /// <summary>Initializes a new instance of the <see cref="ReceiveStockCommandHandler"/> class.</summary>
    public ReceiveStockCommandHandler(
        InventoryStockPolicy policy,
        IInventoryItemRepository inventoryItems,
        IUnitOfWork unitOfWork,
        InventoryTenantResolver tenantResolver)
    {
        _policy = policy;
        _inventoryItems = inventoryItems;
        _unitOfWork = unitOfWork;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> HandleAsync(
        ReceiveStockCommand request,
        CancellationToken cancellationToken = default)
    {
        if (request.ProductId == Guid.Empty)
        {
            return Result.Failure<Guid>(InventoryApplicationErrors.InvalidProductId);
        }

        if (request.WarehouseId == Guid.Empty)
        {
            return Result.Failure<Guid>(InventoryApplicationErrors.InvalidWarehouseId);
        }

        Result<TenantId> tenant = _tenantResolver.Resolve();
        if (tenant.IsFailure)
        {
            return Result.Failure<Guid>(tenant.Error);
        }

        Result<Quantity> quantity = Quantity.Create(request.Quantity);
        if (quantity.IsFailure)
        {
            return Result.Failure<Guid>(quantity.Error);
        }

        Result<InventoryStockResolution> resolution = await _policy
            .ResolveAsync(
                tenant.Value,
                ProductId.From(request.ProductId),
                WarehouseId.From(request.WarehouseId),
                createIfMissing: true,
                cancellationToken)
            .ConfigureAwait(false);

        if (resolution.IsFailure)
        {
            return Result.Failure<Guid>(resolution.Error);
        }

        InventoryItem item = resolution.Value.Item;
        Result received = item.Receive(quantity.Value);
        if (received.IsFailure)
        {
            return Result.Failure<Guid>(received.Error);
        }

        if (resolution.Value.IsNew)
        {
            await _inventoryItems.AddAsync(item, cancellationToken).ConfigureAwait(false);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success(item.Id.Value);
    }
}
