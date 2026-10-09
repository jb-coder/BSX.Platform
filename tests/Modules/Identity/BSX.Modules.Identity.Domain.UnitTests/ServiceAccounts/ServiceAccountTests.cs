using BSX.Modules.Identity.Domain.DomainEvents;
using BSX.Modules.Identity.Domain.ServiceAccounts;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Domain.UnitTests.ServiceAccounts;

public sealed class ServiceAccountTests
{
    [Fact]
    public void Should_Fail_WhenNameIsEmpty()
    {
        Result<ServiceAccount> result = ServiceAccount.Create(ServiceAccountId.New(), TenantId.New(), "  ");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.Name.Invalid");
    }

    [Fact]
    public void Should_Create_Enabled_AndRaiseEvent()
    {
        Result<ServiceAccount> result = ServiceAccount.Create(ServiceAccountId.New(), TenantId.New(), "billing-bot");

        result.IsSuccess.Should().BeTrue();
        result.Value.Enabled.Should().BeTrue();
        result.Value.DomainEvents.Should().ContainSingle(domainEvent => domainEvent is ServiceAccountCreated);
    }

    [Fact]
    public void Should_Disable_And_Enable()
    {
        var account = ServiceAccount.Create(ServiceAccountId.New(), TenantId.New(), "billing-bot").Value;

        account.Disable().IsSuccess.Should().BeTrue();
        account.Enabled.Should().BeFalse();

        account.Enable().IsSuccess.Should().BeTrue();
        account.Enabled.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenDisablingTwice()
    {
        var account = ServiceAccount.Create(ServiceAccountId.New(), TenantId.New(), "billing-bot").Value;
        account.Disable();

        Result result = account.Disable();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.ServiceAccount.AlreadyDisabled");
    }

    [Fact]
    public void Should_Fail_WhenRoleAlreadyAssigned()
    {
        var account = ServiceAccount.Create(ServiceAccountId.New(), TenantId.New(), "billing-bot").Value;
        var roleId = RoleId.New();
        account.AssignRole(roleId);

        Result result = account.AssignRole(roleId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.ServiceAccount.RoleAlreadyAssigned");
    }
}
