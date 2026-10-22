using BSX.Modules.Inventory.Domain.DomainEvents;
using BSX.Modules.Inventory.Domain.Enums;
using BSX.Modules.Inventory.Domain.Errors;
using BSX.Modules.Inventory.Domain.Products;
using BSX.Modules.Inventory.Domain.ValueObjects;

namespace BSX.Modules.Inventory.Domain.UnitTests.Products;

public sealed class ProductTests
{
    [Fact]
    public void Should_CreateAnActiveProduct_AndRaiseProductCreated()
    {
        ProductId id = ProductId.New();
        Sku sku = TestData.SkuOf("SKU-100");

        Product product = Product.Create(id, TestData.Tenant, sku, TestData.ProductNameOf("Widget")).Value;

        product.Id.Should().Be(id);
        product.TenantId.Should().Be(TestData.Tenant);
        product.Sku.Should().Be(sku);
        product.Name.Value.Should().Be("Widget");
        product.Status.Should().Be(ProductStatus.Active);
        product.IsActive.Should().BeTrue();

        ProductCreated created = product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductCreated>().Subject;
        created.ProductId.Should().Be(id);
        created.Sku.Should().Be(sku);
    }

    [Fact]
    public void Should_DeactivateAndActivate_AndRaiseTheLifecycleEvents()
    {
        Product product = TestData.CreateProduct();

        product.Deactivate().IsSuccess.Should().BeTrue();
        product.Status.Should().Be(ProductStatus.Inactive);
        product.IsActive.Should().BeFalse();
        product.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<ProductDeactivated>();

        product.ClearDomainEvents();

        product.Activate().IsSuccess.Should().BeTrue();
        product.Status.Should().Be(ProductStatus.Active);
        product.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<ProductActivated>();
    }

    [Fact]
    public void Should_RejectDeactivatingAnInactiveProduct()
    {
        Product product = TestData.CreateProduct();
        product.Deactivate().IsSuccess.Should().BeTrue();
        product.ClearDomainEvents();

        var result = product.Deactivate();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryErrors.ProductAlreadyInactive);
        product.Status.Should().Be(ProductStatus.Inactive);
        product.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Should_RejectActivatingAnActiveProduct()
    {
        Product product = TestData.CreateProduct();

        var result = product.Activate();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryErrors.ProductAlreadyActive);
        product.Status.Should().Be(ProductStatus.Active);
        product.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Should_RenameInAnyStatus_WithoutRaisingAnEvent()
    {
        Product product = TestData.CreateProduct();
        product.Deactivate().IsSuccess.Should().BeTrue();
        product.ClearDomainEvents();

        var result = product.Rename(TestData.ProductNameOf("Renamed Widget"));

        result.IsSuccess.Should().BeTrue();
        product.Name.Value.Should().Be("Renamed Widget");
        product.Status.Should().Be(ProductStatus.Inactive);
        product.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Should_KeepTenantOwnership_AcrossLifecycleChanges()
    {
        TenantId tenant = TenantId.New();
        Product product = TestData.CreateProduct(tenantId: tenant);

        product.Deactivate().IsSuccess.Should().BeTrue();
        product.Activate().IsSuccess.Should().BeTrue();
        product.Rename(TestData.ProductNameOf("Renamed")).IsSuccess.Should().BeTrue();

        product.TenantId.Should().Be(tenant);
    }
}
