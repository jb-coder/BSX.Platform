using BSX.Modules.Identity.Application.Authentication;
using BSX.Modules.Identity.Application.UnitTests.Fakes;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.UnitTests.Authentication;

public sealed class TenantGuardTests
{
    [Fact]
    public void Should_Allow_WhenTenantMatches()
    {
        var tenantId = Guid.NewGuid();
        var guard = new TenantGuard(new FakeTenantContext(tenantId));

        Result result = guard.Ensure(tenantId);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Should_Deny_WhenTenantMismatch()
    {
        var guard = new TenantGuard(new FakeTenantContext(Guid.NewGuid()));

        Result result = guard.Ensure(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Authorization.TenantForbidden");
    }

    [Fact]
    public void Should_Deny_WhenNoActiveTenant()
    {
        var guard = new TenantGuard(new FakeTenantContext(null));

        Result result = guard.Ensure(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Authorization.TenantForbidden");
    }

    [Fact]
    public void Should_Deny_WhenTargetIsMissing()
    {
        var guard = new TenantGuard(new FakeTenantContext(Guid.NewGuid()));

        Result result = guard.Ensure(null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Authentication.InvalidTenant");
    }
}
