using BSX.Modules.Identity.Application.Authentication;
using BSX.Modules.Identity.Application.ServiceAccounts.CreateServiceAccount;
using BSX.Modules.Identity.Application.UnitTests.Fakes;

namespace BSX.Modules.Identity.Application.UnitTests.ServiceAccounts;

public sealed class CreateServiceAccountCommandHandlerTests
{
    private readonly InMemoryServiceAccountRepository _accounts = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private CreateServiceAccountCommandHandler CreateHandler(Guid activeTenantId)
        => new(_accounts, _unitOfWork, new TenantGuard(new FakeTenantContext(activeTenantId)));

    [Fact]
    public async Task Should_CreateServiceAccount_AndPersist()
    {
        var tenantId = Guid.NewGuid();

        var result = await CreateHandler(tenantId).HandleAsync(new CreateServiceAccountCommand(tenantId, "billing-bot"));

        result.IsSuccess.Should().BeTrue();
        _accounts.Accounts.Single().Enabled.Should().BeTrue();
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_WhenNameIsEmpty()
    {
        var tenantId = Guid.NewGuid();

        var result = await CreateHandler(tenantId).HandleAsync(new CreateServiceAccountCommand(tenantId, "   "));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.Name.Invalid");
    }

    [Fact]
    public async Task Should_Fail_WhenTenantMismatch()
    {
        var result = await CreateHandler(Guid.NewGuid()).HandleAsync(new CreateServiceAccountCommand(Guid.NewGuid(), "billing-bot"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Authorization.TenantForbidden");
    }
}
