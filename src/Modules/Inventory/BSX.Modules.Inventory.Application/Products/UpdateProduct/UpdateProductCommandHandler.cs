using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Inventory.Application.Abstractions;
using BSX.Modules.Inventory.Application.Errors;
using BSX.Modules.Inventory.Domain.Products;
using BSX.Modules.Inventory.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Inventory.Application.Products.UpdateProduct;

/// <summary>Handles <see cref="UpdateProductCommand"/>.</summary>
public sealed class UpdateProductCommandHandler : ICommandHandler<UpdateProductCommand>
{
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _unitOfWork;
    private readonly InventoryTenantResolver _tenantResolver;

    /// <summary>Initializes a new instance of the <see cref="UpdateProductCommandHandler"/> class.</summary>
    public UpdateProductCommandHandler(
        IProductRepository products,
        IUnitOfWork unitOfWork,
        InventoryTenantResolver tenantResolver)
    {
        _products = products;
        _unitOfWork = unitOfWork;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc />
    public async Task<Result> HandleAsync(
        UpdateProductCommand request,
        CancellationToken cancellationToken = default)
    {
        if (request.ProductId == Guid.Empty)
        {
            return Result.Failure(InventoryApplicationErrors.InvalidProductId);
        }

        Result<TenantId> tenant = _tenantResolver.Resolve();
        if (tenant.IsFailure)
        {
            return Result.Failure(tenant.Error);
        }

        Result<ProductName> name = ProductName.Create(request.Name);
        if (name.IsFailure)
        {
            return Result.Failure(name.Error);
        }

        Product? product = await _products
            .GetByIdAsync(tenant.Value, ProductId.From(request.ProductId), cancellationToken)
            .ConfigureAwait(false);

        if (product is null)
        {
            return Result.Failure(InventoryApplicationErrors.ProductNotFound);
        }

        Result renamed = product.Rename(name.Value);
        if (renamed.IsFailure)
        {
            return renamed;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
