using BSX.Modules.Identity.Application.ServiceAccounts.CreateServiceAccount;
using BSX.Modules.Identity.Application.UnitTests.Fakes;

namespace BSX.Modules.Identity.Application.UnitTests.ServiceAccounts;

public sealed class CreateServiceAccountCommandHandlerTests
{
    private readonly InMemoryServiceAccountRepository _accounts = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private CreateServiceAccountCommandHandler CreateHandler() => new(_accounts, _unitOfWork);

    [Fact]
    public async Task Should_CreateServiceAccount_AndPersist()
    {
        var result = await CreateHandler().HandleAsync(new CreateServiceAccountCommand(Guid.NewGuid(), "billing-bot"));

        result.IsSuccess.Should().BeTrue();
        _accounts.Accounts.Single().Enabled.Should().BeTrue();
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_WhenNameIsEmpty()
    {
        var result = await CreateHandler().HandleAsync(new CreateServiceAccountCommand(Guid.NewGuid(), "   "));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.Name.Invalid");
    }
}
