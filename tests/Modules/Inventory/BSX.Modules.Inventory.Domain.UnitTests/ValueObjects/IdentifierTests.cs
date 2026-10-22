using BSX.Modules.Inventory.Domain.ValueObjects;

namespace BSX.Modules.Inventory.Domain.UnitTests.ValueObjects;

public sealed class IdentifierTests
{
    [Fact]
    public void New_Should_CreateANonEmptyIdentifier()
    {
        ProductId.New().Value.Should().NotBe(Guid.Empty);
        WarehouseId.New().Value.Should().NotBe(Guid.Empty);
        InventoryItemId.New().Value.Should().NotBe(Guid.Empty);
        TenantId.New().Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void From_Should_PreserveTheValue_AndBeEqual()
    {
        Guid value = Guid.NewGuid();

        ProductId.From(value).Value.Should().Be(value);
        ProductId.From(value).Should().Be(ProductId.From(value));
    }

    [Fact]
    public void Should_Throw_WhenTheValueIsEmpty()
    {
        Action product = () => ProductId.From(Guid.Empty);
        Action warehouse = () => WarehouseId.From(Guid.Empty);
        Action item = () => InventoryItemId.From(Guid.Empty);
        Action tenant = () => TenantId.From(Guid.Empty);

        product.Should().Throw<ArgumentException>();
        warehouse.Should().Throw<ArgumentException>();
        item.Should().Throw<ArgumentException>();
        tenant.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IdentifiersOfDifferentTypes_Should_NotBeEqual()
    {
        Guid value = Guid.NewGuid();

        ((object)ProductId.From(value)).Should().NotBe(WarehouseId.From(value));
        ((object)WarehouseId.From(value)).Should().NotBe(InventoryItemId.From(value));
    }
}
