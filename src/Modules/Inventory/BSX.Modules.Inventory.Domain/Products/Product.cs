using BSX.Modules.Inventory.Domain.DomainEvents;
using BSX.Modules.Inventory.Domain.Enums;
using BSX.Modules.Inventory.Domain.Errors;
using BSX.Modules.Inventory.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Inventory.Domain.Products;

/// <summary>
/// A sellable or stockable product. SKU uniqueness is enforced by the Infrastructure layer; the
/// aggregate guarantees the active/inactive lifecycle and tenant ownership (ADR-013).
/// </summary>
public sealed class Product : AggregateRoot<ProductId>
{
    private Product(ProductId id, TenantId tenantId, Sku sku, ProductName name)
        : base(id)
    {
        TenantId = tenantId;
        Sku = sku;
        Name = name;
        Status = ProductStatus.Active;
    }

    private Product()
    {
    }

    /// <summary>Gets the tenant identifier.</summary>
    public TenantId TenantId { get; private set; } = null!;

    /// <summary>Gets the tenant-unique SKU.</summary>
    public Sku Sku { get; private set; } = null!;

    /// <summary>Gets the product name.</summary>
    public ProductName Name { get; private set; } = null!;

    /// <summary>Gets the lifecycle status.</summary>
    public ProductStatus Status { get; private set; }

    /// <summary>Gets a value indicating whether the product is active.</summary>
    public bool IsActive => Status == ProductStatus.Active;

    /// <summary>Creates an active product.</summary>
    /// <param name="id">The product identifier.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="sku">The tenant-unique SKU.</param>
    /// <param name="name">The product name.</param>
    public static Result<Product> Create(ProductId id, TenantId tenantId, Sku sku, ProductName name)
    {
        var product = new Product(id, tenantId, sku, name);
        product.RaiseDomainEvent(new ProductCreated(id, sku));
        return Result.Success(product);
    }

    /// <summary>Renames the product. Allowed in any status.</summary>
    /// <param name="name">The new name.</param>
    public Result Rename(ProductName name)
    {
        Name = name;
        return Result.Success();
    }

    /// <summary>Activates an inactive product.</summary>
    public Result Activate()
    {
        if (Status == ProductStatus.Active)
        {
            return Result.Failure(InventoryErrors.ProductAlreadyActive);
        }

        Status = ProductStatus.Active;
        RaiseDomainEvent(new ProductActivated(Id));
        return Result.Success();
    }

    /// <summary>Deactivates an active product.</summary>
    public Result Deactivate()
    {
        if (Status == ProductStatus.Inactive)
        {
            return Result.Failure(InventoryErrors.ProductAlreadyInactive);
        }

        Status = ProductStatus.Inactive;
        RaiseDomainEvent(new ProductDeactivated(Id));
        return Result.Success();
    }
}
