using BSX.Modules.Inventory.Application.Abstractions;
using BSX.Modules.Inventory.Application.Dtos;
using BSX.Modules.Inventory.Application.Errors;
using BSX.Modules.Inventory.Application.Inventory.GetInventoryItemById;
using BSX.Modules.Inventory.Application.Inventory.ListInventoryItems;
using BSX.Modules.Inventory.Application.UnitTests.Fakes;

namespace BSX.Modules.Inventory.Application.UnitTests.Inventory;

public sealed class InventoryQueryHandlerTests
{
    private readonly FakeInventoryReadRepository _inventory = new();

    [Fact]
    public async Task Should_GetInventoryItemById_UsingTenantScope()
    {
        Guid itemId = Guid.NewGuid();
        InventoryItemDto dto = TestData.InventoryItemDto(id: itemId);
        _inventory.Item = dto;

        var handler = new GetInventoryItemByIdQueryHandler(_inventory, TenantResolver());
        var result = await handler.HandleAsync(new GetInventoryItemByIdQuery(itemId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(dto);
        _inventory.LastTenantId.Should().Be(TestData.Tenant);
    }

    [Fact]
    public async Task Should_Fail_WhenInventoryItemProjectionNotFound()
    {
        var handler = new GetInventoryItemByIdQueryHandler(_inventory, TenantResolver());
        var result = await handler.HandleAsync(new GetInventoryItemByIdQuery(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryApplicationErrors.InventoryItemNotFound);
    }

    [Fact]
    public async Task Should_Fail_WhenInventoryItemIdIsEmpty()
    {
        var handler = new GetInventoryItemByIdQueryHandler(_inventory, TenantResolver());
        var result = await handler.HandleAsync(new GetInventoryItemByIdQuery(Guid.Empty));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryApplicationErrors.InvalidInventoryItemId);
        _inventory.LastTenantId.Should().BeNull();
    }

    [Fact]
    public async Task Should_Fail_ToGetInventoryItem_WhenTenantIsMissing()
    {
        var handler = new GetInventoryItemByIdQueryHandler(
            _inventory,
            new InventoryTenantResolver(new FakeTenantContext(null)));

        var result = await handler.HandleAsync(new GetInventoryItemByIdQuery(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryApplicationErrors.TenantRequired);
    }

    [Fact]
    public async Task Should_ListInventoryItems_WithTenantAndNormalizedPaging()
    {
        var handler = new ListInventoryItemsQueryHandler(_inventory, TenantResolver());

        var result = await handler.HandleAsync(new ListInventoryItemsQuery(0, 500, "widget"));

        result.IsSuccess.Should().BeTrue();
        _inventory.LastTenantId.Should().Be(TestData.Tenant);
        _inventory.LastPage.Should().Be(1);
        _inventory.LastSize.Should().Be(20);
        _inventory.LastSearch.Should().Be("widget");
    }

    [Fact]
    public async Task Should_ListInventoryItems_WithRequestedPaging()
    {
        var handler = new ListInventoryItemsQueryHandler(_inventory, TenantResolver());

        await handler.HandleAsync(new ListInventoryItemsQuery(3, 50, null));

        _inventory.LastPage.Should().Be(3);
        _inventory.LastSize.Should().Be(50);
    }

    [Fact]
    public async Task Should_Fail_ToListInventoryItems_WhenTenantIsMissing()
    {
        var handler = new ListInventoryItemsQueryHandler(
            _inventory,
            new InventoryTenantResolver(new FakeTenantContext(null)));

        var result = await handler.HandleAsync(new ListInventoryItemsQuery(1, 20, null));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryApplicationErrors.TenantRequired);
    }

    private InventoryTenantResolver TenantResolver() => new(new FakeTenantContext(TestData.Tenant));
}
