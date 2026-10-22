using BSX.Modules.Inventory.Application.Abstractions;
using BSX.Modules.Inventory.Application.Errors;
using BSX.Modules.Inventory.Application.UnitTests.Fakes;

namespace BSX.Modules.Inventory.Application.UnitTests;

public sealed class TenantResolverTests
{
    [Fact]
    public void Should_ResolveTenantFromContext()
    {
        var resolver = new InventoryTenantResolver(new FakeTenantContext(TestData.Tenant));

        var result = resolver.Resolve();

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(TestData.Tenant);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Should_FailClosed_WhenTenantIsMissingOrEmpty(string? tenantId)
    {
        Guid? value = tenantId is null ? null : Guid.Parse(tenantId);
        var resolver = new InventoryTenantResolver(new FakeTenantContext(value));

        var result = resolver.Resolve();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryApplicationErrors.TenantRequired);
    }
}
