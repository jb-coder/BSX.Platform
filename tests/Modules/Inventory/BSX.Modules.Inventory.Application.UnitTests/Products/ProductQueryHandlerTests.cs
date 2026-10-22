using BSX.Modules.Inventory.Application.Abstractions;
using BSX.Modules.Inventory.Application.Dtos;
using BSX.Modules.Inventory.Application.Errors;
using BSX.Modules.Inventory.Application.Products.GetProductById;
using BSX.Modules.Inventory.Application.Products.ListProducts;
using BSX.Modules.Inventory.Application.UnitTests.Fakes;

namespace BSX.Modules.Inventory.Application.UnitTests.Products;

public sealed class ProductQueryHandlerTests
{
    private readonly FakeProductReadRepository _products = new();

    [Fact]
    public async Task Should_GetProductById_UsingTenantScope()
    {
        Guid productId = Guid.NewGuid();
        ProductDto dto = TestData.ProductDto(id: productId);
        _products.Product = dto;

        var handler = new GetProductByIdQueryHandler(_products, TenantResolver());
        var result = await handler.HandleAsync(new GetProductByIdQuery(productId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(dto);
        _products.LastTenantId.Should().Be(TestData.Tenant);
    }

    [Fact]
    public async Task Should_Fail_WhenProductProjectionNotFound()
    {
        var handler = new GetProductByIdQueryHandler(_products, TenantResolver());
        var result = await handler.HandleAsync(new GetProductByIdQuery(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryApplicationErrors.ProductNotFound);
    }

    [Fact]
    public async Task Should_Fail_WhenProductIdIsEmpty()
    {
        var handler = new GetProductByIdQueryHandler(_products, TenantResolver());
        var result = await handler.HandleAsync(new GetProductByIdQuery(Guid.Empty));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryApplicationErrors.InvalidProductId);
        _products.LastTenantId.Should().BeNull();
    }

    [Fact]
    public async Task Should_Fail_ToGetProduct_WhenTenantIsMissing()
    {
        var handler = new GetProductByIdQueryHandler(
            _products,
            new InventoryTenantResolver(new FakeTenantContext(null)));

        var result = await handler.HandleAsync(new GetProductByIdQuery(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryApplicationErrors.TenantRequired);
    }

    [Fact]
    public async Task Should_ListProducts_WithTenantAndNormalizedPaging()
    {
        var handler = new ListProductsQueryHandler(_products, TenantResolver());

        var result = await handler.HandleAsync(new ListProductsQuery(0, 500, "widget"));

        result.IsSuccess.Should().BeTrue();
        _products.LastTenantId.Should().Be(TestData.Tenant);
        _products.LastPage.Should().Be(1);
        _products.LastSize.Should().Be(20);
        _products.LastSearch.Should().Be("widget");
    }

    [Fact]
    public async Task Should_ListProducts_WithRequestedPaging()
    {
        var handler = new ListProductsQueryHandler(_products, TenantResolver());

        await handler.HandleAsync(new ListProductsQuery(3, 50, null));

        _products.LastPage.Should().Be(3);
        _products.LastSize.Should().Be(50);
    }

    [Fact]
    public async Task Should_Fail_ToListProducts_WhenTenantIsMissing()
    {
        var handler = new ListProductsQueryHandler(
            _products,
            new InventoryTenantResolver(new FakeTenantContext(null)));

        var result = await handler.HandleAsync(new ListProductsQuery(1, 20, null));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryApplicationErrors.TenantRequired);
    }

    private InventoryTenantResolver TenantResolver() => new(new FakeTenantContext(TestData.Tenant));
}
