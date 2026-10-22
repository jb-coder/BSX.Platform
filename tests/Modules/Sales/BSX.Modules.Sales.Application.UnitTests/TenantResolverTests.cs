using BSX.Modules.Sales.Application.Abstractions;
using BSX.Modules.Sales.Application.Errors;
using BSX.Modules.Sales.Application.UnitTests.Fakes;
using BSX.Modules.Sales.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Sales.Application.UnitTests;

public sealed class TenantResolverTests
{
    [Fact]
    public void Should_Resolve_TenantFromContext()
    {
        var resolver = new SalesTenantResolver(new FakeTenantContext(TestData.Tenant));

        Result<TenantId> result = resolver.Resolve();

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(TestData.Tenant);
    }

    [Fact]
    public void Should_Fail_WhenContextHasNoTenant()
    {
        var resolver = new SalesTenantResolver(new FakeTenantContext(null));

        Result<TenantId> result = resolver.Resolve();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesApplicationErrors.TenantRequired);
    }

    [Fact]
    public void Should_Fail_WhenContextTenantIsEmpty()
    {
        var resolver = new SalesTenantResolver(new FakeTenantContext(Guid.Empty));

        Result<TenantId> result = resolver.Resolve();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesApplicationErrors.TenantRequired);
    }
}
