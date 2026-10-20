using BSX.Modules.Crm.Application.Abstractions;
using BSX.Modules.Crm.Application.Errors;
using BSX.Modules.Crm.Application.UnitTests.Fakes;
using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Crm.Application.UnitTests;

public sealed class TenantResolverTests
{
    [Fact]
    public void Should_Resolve_TenantFromContext()
    {
        var resolver = new CrmTenantResolver(new FakeTenantContext(TestData.Tenant));

        Result<TenantId> result = resolver.Resolve();

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(TestData.Tenant);
    }

    [Fact]
    public void Should_Fail_WhenContextHasNoTenant()
    {
        var resolver = new CrmTenantResolver(new FakeTenantContext(null));

        Result<TenantId> result = resolver.Resolve();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmApplicationErrors.TenantRequired);
    }

    [Fact]
    public void Should_Fail_WhenContextTenantIsEmpty()
    {
        var resolver = new CrmTenantResolver(new FakeTenantContext(Guid.Empty));

        Result<TenantId> result = resolver.Resolve();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmApplicationErrors.TenantRequired);
    }
}
