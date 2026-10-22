using BSX.Modules.Inventory.Application.Abstractions;
using BSX.Modules.Inventory.Application.Common;
using BSX.Modules.Inventory.Application.Errors;
using BSX.Modules.Inventory.Application.Inventory.AdjustStock;
using BSX.Modules.Inventory.Application.Inventory.ReceiveStock;
using BSX.Modules.Inventory.Application.Inventory.ReleaseReservation;
using BSX.Modules.Inventory.Application.Inventory.ReserveStock;
using BSX.Modules.Inventory.Application.UnitTests.Fakes;
using BSX.Modules.Inventory.Domain.Errors;
using BSX.Modules.Inventory.Domain.InventoryItems;
using BSX.Modules.Inventory.Domain.Products;
using BSX.Modules.Inventory.Domain.Warehouses;

namespace BSX.Modules.Inventory.Application.UnitTests.Inventory;

public sealed class InventoryStockCommandHandlerTests
{
    private readonly InMemoryProductRepository _products = new();
    private readonly InMemoryWarehouseRepository _warehouses = new();
    private readonly InMemoryInventoryItemRepository _items = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task Should_CreateStockPosition_WhenReceivingIntoAnEmptyPair()
    {
        Product product = TestData.CreateProduct();
        Warehouse warehouse = TestData.CreateWarehouse();
        _products.Seed(product);
        _warehouses.Seed(warehouse);

        var result = await ReceiveHandler().HandleAsync(
            new ReceiveStockCommand(product.Id.Value, warehouse.Id.Value, 10));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        InventoryItem item = _items.Items.Should().ContainSingle().Subject;
        item.Id.Value.Should().Be(result.Value);
        item.ProductId.Should().Be(product.Id);
        item.WarehouseId.Should().Be(warehouse.Id);
        item.TenantId.Value.Should().Be(TestData.Tenant);
        item.OnHandQuantity.Value.Should().Be(10);
        item.ReservedQuantity.Value.Should().Be(0);
        _products.LastRequestedTenant!.Value.Should().Be(TestData.Tenant);
        _warehouses.LastRequestedTenant!.Value.Should().Be(TestData.Tenant);
        _items.LastRequestedTenant!.Value.Should().Be(TestData.Tenant);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_ReceiveIntoExistingPosition_WithoutCreatingAnother()
    {
        Product product = TestData.CreateProduct();
        Warehouse warehouse = TestData.CreateWarehouse();
        InventoryItem item = TestData.CreateItem(
            productId: product.Id.Value,
            warehouseId: warehouse.Id.Value,
            onHand: 10);
        _products.Seed(product);
        _warehouses.Seed(warehouse);
        _items.Seed(item);

        var result = await ReceiveHandler().HandleAsync(
            new ReceiveStockCommand(product.Id.Value, warehouse.Id.Value, 5));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(item.Id.Value);
        item.OnHandQuantity.Value.Should().Be(15);
        _items.Items.Should().ContainSingle();
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_FailReceive_WhenProductDoesNotExist()
    {
        Warehouse warehouse = TestData.CreateWarehouse();
        _warehouses.Seed(warehouse);

        var result = await ReceiveHandler().HandleAsync(
            new ReceiveStockCommand(Guid.NewGuid(), warehouse.Id.Value, 10));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryApplicationErrors.ProductNotFound);
        _warehouses.LastRequestedTenant.Should().BeNull("the policy must fail before loading the warehouse");
        _items.Items.Should().BeEmpty();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_FailReceive_WhenProductBelongsToAnotherTenant()
    {
        Product product = TestData.CreateProduct(tenantId: Guid.NewGuid());
        Warehouse warehouse = TestData.CreateWarehouse();
        _products.Seed(product);
        _warehouses.Seed(warehouse);

        var result = await ReceiveHandler().HandleAsync(
            new ReceiveStockCommand(product.Id.Value, warehouse.Id.Value, 10));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryApplicationErrors.ProductNotFound);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_FailReceive_WhenProductIsInactive()
    {
        Product product = TestData.CreateProduct(active: false);
        Warehouse warehouse = TestData.CreateWarehouse();
        _products.Seed(product);
        _warehouses.Seed(warehouse);

        var result = await ReceiveHandler().HandleAsync(
            new ReceiveStockCommand(product.Id.Value, warehouse.Id.Value, 10));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryApplicationErrors.ProductInactive);
        _warehouses.LastRequestedTenant.Should().BeNull("the policy must fail before loading the warehouse");
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_FailReceive_WhenWarehouseDoesNotExist()
    {
        Product product = TestData.CreateProduct();
        _products.Seed(product);

        var result = await ReceiveHandler().HandleAsync(
            new ReceiveStockCommand(product.Id.Value, Guid.NewGuid(), 10));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryApplicationErrors.WarehouseNotFound);
        _items.Items.Should().BeEmpty();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_FailReceive_WhenWarehouseIsInactive()
    {
        Product product = TestData.CreateProduct();
        Warehouse warehouse = TestData.CreateWarehouse(active: false);
        _products.Seed(product);
        _warehouses.Seed(warehouse);

        var result = await ReceiveHandler().HandleAsync(
            new ReceiveStockCommand(product.Id.Value, warehouse.Id.Value, 10));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryApplicationErrors.WarehouseInactive);
        _items.Items.Should().BeEmpty();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_FailReceive_WhenQuantityIsZero()
    {
        Product product = TestData.CreateProduct();
        Warehouse warehouse = TestData.CreateWarehouse();
        _products.Seed(product);
        _warehouses.Seed(warehouse);

        var result = await ReceiveHandler().HandleAsync(
            new ReceiveStockCommand(product.Id.Value, warehouse.Id.Value, 0));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryErrors.StockQuantityNotPositive);
        _items.Items.Should().BeEmpty();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_FailReceive_WhenIdentifiersAreEmpty()
    {
        var missingProduct = await ReceiveHandler().HandleAsync(
            new ReceiveStockCommand(Guid.Empty, Guid.NewGuid(), 10));
        var missingWarehouse = await ReceiveHandler().HandleAsync(
            new ReceiveStockCommand(Guid.NewGuid(), Guid.Empty, 10));

        missingProduct.Error.Should().Be(InventoryApplicationErrors.InvalidProductId);
        missingWarehouse.Error.Should().Be(InventoryApplicationErrors.InvalidWarehouseId);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_FailStockOperations_WhenTenantIsMissing()
    {
        var handler = new ReceiveStockCommandHandler(
            Policy(),
            _items,
            _unitOfWork,
            new InventoryTenantResolver(new FakeTenantContext(null)));

        var result = await handler.HandleAsync(new ReceiveStockCommand(Guid.NewGuid(), Guid.NewGuid(), 10));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryApplicationErrors.TenantRequired);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_ReserveStock_UsingTheApplicationPolicy()
    {
        Product product = TestData.CreateProduct();
        Warehouse warehouse = TestData.CreateWarehouse();
        InventoryItem item = TestData.CreateItem(
            productId: product.Id.Value,
            warehouseId: warehouse.Id.Value,
            onHand: 100,
            reserved: 20);
        _products.Seed(product);
        _warehouses.Seed(warehouse);
        _items.Seed(item);

        var result = await ReserveHandler().HandleAsync(
            new ReserveStockCommand(product.Id.Value, warehouse.Id.Value, 30));

        result.IsSuccess.Should().BeTrue();
        item.ReservedQuantity.Value.Should().Be(50);
        item.AvailableQuantity.Value.Should().Be(50);
        _products.LastRequestedTenant!.Value.Should().Be(TestData.Tenant);
        _warehouses.LastRequestedTenant!.Value.Should().Be(TestData.Tenant);
        _items.LastRequestedTenant!.Value.Should().Be(TestData.Tenant);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_FailReserve_WhenWarehouseBelongsToAnotherTenant()
    {
        Product product = TestData.CreateProduct();
        Warehouse warehouse = TestData.CreateWarehouse(tenantId: Guid.NewGuid());
        _products.Seed(product);
        _warehouses.Seed(warehouse);

        var result = await ReserveHandler().HandleAsync(
            new ReserveStockCommand(product.Id.Value, warehouse.Id.Value, 10));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryApplicationErrors.WarehouseNotFound);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_FailReserve_WhenStockPositionDoesNotExist()
    {
        Product product = TestData.CreateProduct();
        Warehouse warehouse = TestData.CreateWarehouse();
        _products.Seed(product);
        _warehouses.Seed(warehouse);

        var result = await ReserveHandler().HandleAsync(
            new ReserveStockCommand(product.Id.Value, warehouse.Id.Value, 10));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryApplicationErrors.InventoryItemNotFound);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_Propagate_WhenReservingMoreThanAvailable()
    {
        Product product = TestData.CreateProduct();
        Warehouse warehouse = TestData.CreateWarehouse();
        InventoryItem item = TestData.CreateItem(
            productId: product.Id.Value,
            warehouseId: warehouse.Id.Value,
            onHand: 100,
            reserved: 80);
        _products.Seed(product);
        _warehouses.Seed(warehouse);
        _items.Seed(item);

        var result = await ReserveHandler().HandleAsync(
            new ReserveStockCommand(product.Id.Value, warehouse.Id.Value, 21));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryErrors.InsufficientAvailableStock);
        item.ReservedQuantity.Value.Should().Be(80);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_EnforceProductAndWarehousePolicy_ForReserve()
    {
        Product inactiveProduct = TestData.CreateProduct(active: false);
        Product activeProduct = TestData.CreateProduct(sku: "SKU-002");
        Warehouse inactiveWarehouse = TestData.CreateWarehouse(active: false);
        _products.Seed(inactiveProduct, activeProduct);
        _warehouses.Seed(inactiveWarehouse);

        var inactiveProductResult = await ReserveHandler().HandleAsync(
            new ReserveStockCommand(inactiveProduct.Id.Value, inactiveWarehouse.Id.Value, 10));
        var inactiveWarehouseResult = await ReserveHandler().HandleAsync(
            new ReserveStockCommand(activeProduct.Id.Value, inactiveWarehouse.Id.Value, 10));

        inactiveProductResult.Error.Should().Be(InventoryApplicationErrors.ProductInactive);
        inactiveWarehouseResult.Error.Should().Be(InventoryApplicationErrors.WarehouseInactive);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_ReleaseReservation()
    {
        Product product = TestData.CreateProduct();
        Warehouse warehouse = TestData.CreateWarehouse();
        InventoryItem item = TestData.CreateItem(
            productId: product.Id.Value,
            warehouseId: warehouse.Id.Value,
            onHand: 100,
            reserved: 30);
        _products.Seed(product);
        _warehouses.Seed(warehouse);
        _items.Seed(item);

        var result = await ReleaseHandler().HandleAsync(
            new ReleaseReservationCommand(product.Id.Value, warehouse.Id.Value, 10));

        result.IsSuccess.Should().BeTrue();
        item.ReservedQuantity.Value.Should().Be(20);
        item.AvailableQuantity.Value.Should().Be(80);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Propagate_WhenReleasingMoreThanReserved()
    {
        Product product = TestData.CreateProduct();
        Warehouse warehouse = TestData.CreateWarehouse();
        InventoryItem item = TestData.CreateItem(
            productId: product.Id.Value,
            warehouseId: warehouse.Id.Value,
            onHand: 100,
            reserved: 10);
        _products.Seed(product);
        _warehouses.Seed(warehouse);
        _items.Seed(item);

        var result = await ReleaseHandler().HandleAsync(
            new ReleaseReservationCommand(product.Id.Value, warehouse.Id.Value, 11));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryErrors.InsufficientReservedStock);
        item.ReservedQuantity.Value.Should().Be(10);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(-10)]
    public async Task Should_AdjustStock(int delta)
    {
        Product product = TestData.CreateProduct();
        Warehouse warehouse = TestData.CreateWarehouse();
        InventoryItem item = TestData.CreateItem(
            productId: product.Id.Value,
            warehouseId: warehouse.Id.Value,
            onHand: 100,
            reserved: 20);
        _products.Seed(product);
        _warehouses.Seed(warehouse);
        _items.Seed(item);

        var result = await AdjustHandler().HandleAsync(
            new AdjustStockCommand(product.Id.Value, warehouse.Id.Value, delta));

        result.IsSuccess.Should().BeTrue();
        item.OnHandQuantity.Value.Should().Be(100 + delta);
        item.ReservedQuantity.Value.Should().Be(20);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Propagate_WhenAdjustmentWouldFallBelowReserved()
    {
        Product product = TestData.CreateProduct();
        Warehouse warehouse = TestData.CreateWarehouse();
        InventoryItem item = TestData.CreateItem(
            productId: product.Id.Value,
            warehouseId: warehouse.Id.Value,
            onHand: 100,
            reserved: 80);
        _products.Seed(product);
        _warehouses.Seed(warehouse);
        _items.Seed(item);

        var result = await AdjustHandler().HandleAsync(
            new AdjustStockCommand(product.Id.Value, warehouse.Id.Value, -30));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryErrors.StockAdjustmentBelowReserved);
        item.OnHandQuantity.Value.Should().Be(100);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_Propagate_WhenAdjustmentIsZero()
    {
        Product product = TestData.CreateProduct();
        Warehouse warehouse = TestData.CreateWarehouse();
        InventoryItem item = TestData.CreateItem(
            productId: product.Id.Value,
            warehouseId: warehouse.Id.Value,
            onHand: 100);
        _products.Seed(product);
        _warehouses.Seed(warehouse);
        _items.Seed(item);

        var result = await AdjustHandler().HandleAsync(
            new AdjustStockCommand(product.Id.Value, warehouse.Id.Value, 0));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryErrors.StockAdjustmentIsZero);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task ReleaseAndAdjust_Should_RequireAnExistingStockPosition()
    {
        Product product = TestData.CreateProduct();
        Warehouse warehouse = TestData.CreateWarehouse();
        _products.Seed(product);
        _warehouses.Seed(warehouse);

        var released = await ReleaseHandler().HandleAsync(
            new ReleaseReservationCommand(product.Id.Value, warehouse.Id.Value, 10));
        var adjusted = await AdjustHandler().HandleAsync(
            new AdjustStockCommand(product.Id.Value, warehouse.Id.Value, 10));

        released.Error.Should().Be(InventoryApplicationErrors.InventoryItemNotFound);
        adjusted.Error.Should().Be(InventoryApplicationErrors.InventoryItemNotFound);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    private InventoryTenantResolver TenantResolver() => new(new FakeTenantContext(TestData.Tenant));

    private InventoryStockPolicy Policy() => new(_products, _warehouses, _items);

    private ReceiveStockCommandHandler ReceiveHandler()
        => new(Policy(), _items, _unitOfWork, TenantResolver());

    private ReserveStockCommandHandler ReserveHandler()
        => new(Policy(), _unitOfWork, TenantResolver());

    private ReleaseReservationCommandHandler ReleaseHandler()
        => new(Policy(), _unitOfWork, TenantResolver());

    private AdjustStockCommandHandler AdjustHandler()
        => new(Policy(), _unitOfWork, TenantResolver());
}

