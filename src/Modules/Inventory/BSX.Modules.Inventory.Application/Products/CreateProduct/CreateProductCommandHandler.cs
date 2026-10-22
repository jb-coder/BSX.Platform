using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Inventory.Application.Abstractions;
using BSX.Modules.Inventory.Application.Errors;
using BSX.Modules.Inventory.Domain.Products;
using BSX.Modules.Inventory.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Inventory.Application.Products.CreateProduct;

/// <summary>Handles <see cref="CreateProductCommand"/>.</summary>
public sealed class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Guid>
{
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _unitOfWork;
    private readonly InventoryTenantResolver _tenantResolver;

    /// <summary>Initializes a new instance of the <see cref="CreateProductCommandHandler"/> class.</summary>
    public CreateProductCommandHandler(
        IProductRepository products,
        IUnitOfWork unitOfWork,
        InventoryTenantResolver tenantResolver)
    {
        _products = products;
        _unitOfWork = unitOfWork;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> HandleAsync(
        CreateProductCommand request,
        CancellationToken cancellationToken = default)
    {
        Result<TenantId> tenant = _tenantResolver.Resolve();
        if (tenant.IsFailure)
        {
            return Result.Failure<Guid>(tenant.Error);
        }

        Result<Sku> sku = Sku.Create(request.Sku);
        if (sku.IsFailure)
        {
            return Result.Failure<Guid>(sku.Error);
        }

        Result<ProductName> name = ProductName.Create(request.Name);
        if (name.IsFailure)
        {
            return Result.Failure<Guid>(name.Error);
        }

        if (await _products.ExistsBySkuAsync(tenant.Value, sku.Value, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure<Guid>(InventoryApplicationErrors.ProductSkuAlreadyExists);
        }

        Result<Product> created = Product.Create(ProductId.New(), tenant.Value, sku.Value, name.Value);
        if (created.IsFailure)
        {
            return Result.Failure<Guid>(created.Error);
        }

        await _products.AddAsync(created.Value, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(created.Value.Id.Value);
    }
}
