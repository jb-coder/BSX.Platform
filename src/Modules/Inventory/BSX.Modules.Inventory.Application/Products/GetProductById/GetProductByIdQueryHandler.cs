using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Inventory.Application.Abstractions;
using BSX.Modules.Inventory.Application.Dtos;
using BSX.Modules.Inventory.Application.Errors;
using BSX.Modules.Inventory.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Inventory.Application.Products.GetProductById;

/// <summary>Handles <see cref="GetProductByIdQuery"/>. Projects through the read model; loads no aggregate.</summary>
public sealed class GetProductByIdQueryHandler : IQueryHandler<GetProductByIdQuery, ProductDto>
{
    private readonly IProductReadRepository _products;
    private readonly InventoryTenantResolver _tenantResolver;

    /// <summary>Initializes a new instance of the <see cref="GetProductByIdQueryHandler"/> class.</summary>
    public GetProductByIdQueryHandler(IProductReadRepository products, InventoryTenantResolver tenantResolver)
    {
        _products = products;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc />
    public async Task<Result<ProductDto>> HandleAsync(
        GetProductByIdQuery request,
        CancellationToken cancellationToken = default)
    {
        if (request.ProductId == Guid.Empty)
        {
            return Result.Failure<ProductDto>(InventoryApplicationErrors.InvalidProductId);
        }

        Result<TenantId> tenant = _tenantResolver.Resolve();
        if (tenant.IsFailure)
        {
            return Result.Failure<ProductDto>(tenant.Error);
        }

        ProductDto? product = await _products
            .GetByIdAsync(tenant.Value.Value, request.ProductId, cancellationToken)
            .ConfigureAwait(false);

        return product is null
            ? Result.Failure<ProductDto>(InventoryApplicationErrors.ProductNotFound)
            : Result.Success(product);
    }
}
