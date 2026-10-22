using BSX.Modules.Inventory.Application.Inventory.AdjustStock;
using BSX.Modules.Inventory.Application.Inventory.ReceiveStock;
using BSX.Modules.Inventory.Application.Inventory.ReleaseReservation;
using BSX.Modules.Inventory.Application.Inventory.ReserveStock;
using BSX.Modules.Inventory.Application.Products.ActivateProduct;
using BSX.Modules.Inventory.Application.Products.CreateProduct;
using BSX.Modules.Inventory.Application.Products.DeactivateProduct;
using BSX.Modules.Inventory.Application.Products.UpdateProduct;
using BSX.Modules.Inventory.Application.Warehouses.ActivateWarehouse;
using BSX.Modules.Inventory.Application.Warehouses.CreateWarehouse;
using BSX.Modules.Inventory.Application.Warehouses.DeactivateWarehouse;
using BSX.Modules.Inventory.Application.Warehouses.UpdateWarehouse;
using BSX.Modules.Inventory.Domain.ValueObjects;

namespace BSX.Modules.Inventory.Application.UnitTests.Validators;

public sealed class InventoryCommandValidatorTests
{
    [Fact]
    public async Task CreateProduct_Should_BeValid_WhenValid()
    {
        var result = await new CreateProductCommandValidator().ValidateAsync(
            new CreateProductCommand("SKU-001", "Widget"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("SKU-001", true)]
    public async Task CreateProduct_Should_ValidateSku(string sku, bool expected)
    {
        var result = await new CreateProductCommandValidator().ValidateAsync(
            new CreateProductCommand(sku, "Widget"));

        result.IsValid.Should().Be(expected);
    }

    [Fact]
    public async Task CreateProduct_Should_BeInvalid_WhenFieldsExceedMaximumLengths()
    {
        var longSku = await new CreateProductCommandValidator().ValidateAsync(
            new CreateProductCommand(new string('S', Sku.MaxLength + 1), "Widget"));
        var longName = await new CreateProductCommandValidator().ValidateAsync(
            new CreateProductCommand("SKU-001", new string('n', ProductName.MaxLength + 1)));

        longSku.IsValid.Should().BeFalse();
        longName.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateProduct_Should_ValidateShape()
    {
        var valid = await new UpdateProductCommandValidator().ValidateAsync(
            new UpdateProductCommand(Guid.NewGuid(), "Widget"));
        var emptyId = await new UpdateProductCommandValidator().ValidateAsync(
            new UpdateProductCommand(Guid.Empty, "Widget"));
        var emptyName = await new UpdateProductCommandValidator().ValidateAsync(
            new UpdateProductCommand(Guid.NewGuid(), " "));

        valid.IsValid.Should().BeTrue();
        emptyId.IsValid.Should().BeFalse();
        emptyName.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(typeof(ActivateProductCommand))]
    [InlineData(typeof(DeactivateProductCommand))]
    public async Task ProductLifecycleCommands_Should_BeInvalid_WhenIdIsEmpty(Type commandType)
    {
        FluentValidation.Results.ValidationResult result = commandType.Name switch
        {
            nameof(ActivateProductCommand) => await new ActivateProductCommandValidator()
                .ValidateAsync(new ActivateProductCommand(Guid.Empty)),
            _ => await new DeactivateProductCommandValidator()
                .ValidateAsync(new DeactivateProductCommand(Guid.Empty)),
        };

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CreateWarehouse_Should_ValidateShape()
    {
        var valid = await new CreateWarehouseCommandValidator().ValidateAsync(
            new CreateWarehouseCommand("Main Warehouse"));
        var empty = await new CreateWarehouseCommandValidator().ValidateAsync(
            new CreateWarehouseCommand(" "));
        var longName = await new CreateWarehouseCommandValidator().ValidateAsync(
            new CreateWarehouseCommand(new string('n', WarehouseName.MaxLength + 1)));

        valid.IsValid.Should().BeTrue();
        empty.IsValid.Should().BeFalse();
        longName.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateWarehouse_Should_ValidateShape()
    {
        var valid = await new UpdateWarehouseCommandValidator().ValidateAsync(
            new UpdateWarehouseCommand(Guid.NewGuid(), "Main Warehouse"));
        var emptyId = await new UpdateWarehouseCommandValidator().ValidateAsync(
            new UpdateWarehouseCommand(Guid.Empty, "Main Warehouse"));
        var emptyName = await new UpdateWarehouseCommandValidator().ValidateAsync(
            new UpdateWarehouseCommand(Guid.NewGuid(), " "));

        valid.IsValid.Should().BeTrue();
        emptyId.IsValid.Should().BeFalse();
        emptyName.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(typeof(ActivateWarehouseCommand))]
    [InlineData(typeof(DeactivateWarehouseCommand))]
    public async Task WarehouseLifecycleCommands_Should_BeInvalid_WhenIdIsEmpty(Type commandType)
    {
        FluentValidation.Results.ValidationResult result = commandType.Name switch
        {
            nameof(ActivateWarehouseCommand) => await new ActivateWarehouseCommandValidator()
                .ValidateAsync(new ActivateWarehouseCommand(Guid.Empty)),
            _ => await new DeactivateWarehouseCommandValidator()
                .ValidateAsync(new DeactivateWarehouseCommand(Guid.Empty)),
        };

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(typeof(ReceiveStockCommand))]
    [InlineData(typeof(ReserveStockCommand))]
    [InlineData(typeof(ReleaseReservationCommand))]
    [InlineData(typeof(AdjustStockCommand))]
    public async Task StockCommands_Should_BeInvalid_WhenIdentifiersAreEmpty(Type commandType)
    {
        FluentValidation.Results.ValidationResult missingProduct = commandType.Name switch
        {
            nameof(ReceiveStockCommand) => await new ReceiveStockCommandValidator()
                .ValidateAsync(new ReceiveStockCommand(Guid.Empty, Guid.NewGuid(), 10)),
            nameof(ReserveStockCommand) => await new ReserveStockCommandValidator()
                .ValidateAsync(new ReserveStockCommand(Guid.Empty, Guid.NewGuid(), 10)),
            nameof(ReleaseReservationCommand) => await new ReleaseReservationCommandValidator()
                .ValidateAsync(new ReleaseReservationCommand(Guid.Empty, Guid.NewGuid(), 10)),
            _ => await new AdjustStockCommandValidator()
                .ValidateAsync(new AdjustStockCommand(Guid.Empty, Guid.NewGuid(), 10)),
        };

        FluentValidation.Results.ValidationResult missingWarehouse = commandType.Name switch
        {
            nameof(ReceiveStockCommand) => await new ReceiveStockCommandValidator()
                .ValidateAsync(new ReceiveStockCommand(Guid.NewGuid(), Guid.Empty, 10)),
            nameof(ReserveStockCommand) => await new ReserveStockCommandValidator()
                .ValidateAsync(new ReserveStockCommand(Guid.NewGuid(), Guid.Empty, 10)),
            nameof(ReleaseReservationCommand) => await new ReleaseReservationCommandValidator()
                .ValidateAsync(new ReleaseReservationCommand(Guid.NewGuid(), Guid.Empty, 10)),
            _ => await new AdjustStockCommandValidator()
                .ValidateAsync(new AdjustStockCommand(Guid.NewGuid(), Guid.Empty, 10)),
        };

        missingProduct.IsValid.Should().BeFalse();
        missingWarehouse.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task StockCommands_Should_LeaveQuantityRulesToTheDomain()
    {
        var zeroQuantity = await new ReceiveStockCommandValidator().ValidateAsync(
            new ReceiveStockCommand(Guid.NewGuid(), Guid.NewGuid(), 0));
        var zeroDelta = await new AdjustStockCommandValidator().ValidateAsync(
            new AdjustStockCommand(Guid.NewGuid(), Guid.NewGuid(), 0));

        zeroQuantity.IsValid.Should().BeTrue();
        zeroDelta.IsValid.Should().BeTrue();
    }
}
