using BSX.Modules.Inventory.Application.Abstractions;
using BSX.Modules.Inventory.Application.Errors;
using BSX.Modules.Inventory.Application.Products.ActivateProduct;
using BSX.Modules.Inventory.Application.Products.CreateProduct;
using BSX.Modules.Inventory.Application.Products.DeactivateProduct;
using BSX.Modules.Inventory.Application.Products.UpdateProduct;
using BSX.Modules.Inventory.Application.UnitTests.Fakes;
using BSX.Modules.Inventory.Domain.Errors;
using BSX.Modules.Inventory.Domain.Products;

namespace BSX.Modules.Inventory.Application.UnitTests.Products;

public sealed class ProductCommandHandlerTests
{
    private readonly InMemoryProductRepository _products = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task Should_CreateProduct_AndPersist()
    {
        var result = await CreateHandler().HandleAsync(
            new CreateProductCommand("  sku-001  ", "  Widget  "));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        Product product = _products.Products.Should().ContainSingle().Subject;
        product.Id.Value.Should().Be(result.Value);
        product.Sku.Value.Should().Be("SKU-001");
        product.Name.Value.Should().Be("Widget");
        product.TenantId.Value.Should().Be(TestData.Tenant);
        product.IsActive.Should().BeTrue();
        _products.LastRequestedTenant!.Value.Should().Be(TestData.Tenant);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_WhenSkuAlreadyExistsInTenant()
    {
        _products.Seed(TestData.CreateProduct(sku: "SKU-001"));

        var result = await CreateHandler().HandleAsync(new CreateProductCommand("sku-001", "Other"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryApplicationErrors.ProductSkuAlreadyExists);
        _products.Products.Should().ContainSingle();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_AllowSameSku_InAnotherTenant()
    {
        _products.Seed(TestData.CreateProduct(sku: "SKU-001", tenantId: Guid.NewGuid()));

        var result = await CreateHandler().HandleAsync(new CreateProductCommand("SKU-001", "Widget"));

        result.IsSuccess.Should().BeTrue();
        _products.Products.Should().HaveCount(2);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Propagate_WhenSkuIsInvalid()
    {
        var result = await CreateHandler().HandleAsync(new CreateProductCommand("bad sku", "Widget"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryErrors.SkuInvalid);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_Propagate_WhenNameIsInvalid()
    {
        var result = await CreateHandler().HandleAsync(new CreateProductCommand("SKU-001", " "));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryErrors.ProductNameInvalid);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_Fail_WhenTenantIsMissing()
    {
        var handler = new CreateProductCommandHandler(
            _products,
            _unitOfWork,
            new InventoryTenantResolver(new FakeTenantContext(null)));

        var result = await handler.HandleAsync(new CreateProductCommand("SKU-001", "Widget"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryApplicationErrors.TenantRequired);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_UpdateProductName_UsingTenantScope()
    {
        Guid productId = Guid.NewGuid();
        Product product = TestData.CreateProduct(id: productId);
        _products.Seed(product);

        var result = await UpdateHandler().HandleAsync(new UpdateProductCommand(productId, "Renamed"));

        result.IsSuccess.Should().BeTrue();
        product.Name.Value.Should().Be("Renamed");
        _products.LastRequestedTenant!.Value.Should().Be(TestData.Tenant);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_WhenUpdatingUnknownProduct()
    {
        var result = await UpdateHandler().HandleAsync(new UpdateProductCommand(Guid.NewGuid(), "Renamed"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryApplicationErrors.ProductNotFound);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_Fail_WhenProductBelongsToAnotherTenant()
    {
        Product product = TestData.CreateProduct(tenantId: Guid.NewGuid());
        _products.Seed(product);

        var result = await UpdateHandler().HandleAsync(new UpdateProductCommand(product.Id.Value, "Renamed"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryApplicationErrors.ProductNotFound);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_Fail_WhenUpdatingWithEmptyProductId()
    {
        var result = await UpdateHandler().HandleAsync(new UpdateProductCommand(Guid.Empty, "Renamed"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryApplicationErrors.InvalidProductId);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_ActivateInactiveProduct()
    {
        Guid productId = Guid.NewGuid();
        Product product = TestData.CreateProduct(id: productId, active: false);
        _products.Seed(product);

        var result = await ActivateHandler().HandleAsync(new ActivateProductCommand(productId));

        result.IsSuccess.Should().BeTrue();
        product.IsActive.Should().BeTrue();
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Propagate_WhenActivatingAnActiveProduct()
    {
        Guid productId = Guid.NewGuid();
        _products.Seed(TestData.CreateProduct(id: productId));

        var result = await ActivateHandler().HandleAsync(new ActivateProductCommand(productId));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryErrors.ProductAlreadyActive);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_DeactivateActiveProduct()
    {
        Guid productId = Guid.NewGuid();
        Product product = TestData.CreateProduct(id: productId);
        _products.Seed(product);

        var result = await DeactivateHandler().HandleAsync(new DeactivateProductCommand(productId));

        result.IsSuccess.Should().BeTrue();
        product.IsActive.Should().BeFalse();
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Propagate_WhenDeactivatingAnInactiveProduct()
    {
        Guid productId = Guid.NewGuid();
        _products.Seed(TestData.CreateProduct(id: productId, active: false));

        var result = await DeactivateHandler().HandleAsync(new DeactivateProductCommand(productId));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryErrors.ProductAlreadyInactive);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task ProductLifecycleCommands_Should_Fail_WhenProductIsNotFound()
    {
        Guid productId = Guid.NewGuid();

        var activated = await ActivateHandler().HandleAsync(new ActivateProductCommand(productId));
        var deactivated = await DeactivateHandler().HandleAsync(new DeactivateProductCommand(productId));

        activated.Error.Should().Be(InventoryApplicationErrors.ProductNotFound);
        deactivated.Error.Should().Be(InventoryApplicationErrors.ProductNotFound);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    private InventoryTenantResolver TenantResolver() => new(new FakeTenantContext(TestData.Tenant));

    private CreateProductCommandHandler CreateHandler() => new(_products, _unitOfWork, TenantResolver());

    private UpdateProductCommandHandler UpdateHandler() => new(_products, _unitOfWork, TenantResolver());

    private ActivateProductCommandHandler ActivateHandler() => new(_products, _unitOfWork, TenantResolver());

    private DeactivateProductCommandHandler DeactivateHandler() => new(_products, _unitOfWork, TenantResolver());
}
