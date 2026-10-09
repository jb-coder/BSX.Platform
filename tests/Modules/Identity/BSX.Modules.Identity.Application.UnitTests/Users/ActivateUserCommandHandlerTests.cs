using BSX.Modules.Identity.Application.UnitTests.Fakes;
using BSX.Modules.Identity.Application.Users.ActivateUser;
using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.UnitTests.Users;

public sealed class ActivateUserCommandHandlerTests
{
    private readonly InMemoryUserRepository _users = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly TestTimeProvider _timeProvider = new(TestData.Now);

    private ActivateUserCommandHandler CreateHandler() => new(_users, _unitOfWork, _timeProvider);

    [Fact]
    public async Task Should_Activate_WhenCredentialExists()
    {
        User user = User.Register(UserId.New(), TestData.CreateEmail(), TestData.CreateName(), TestData.Now).Value;
        user.SetPassword(TestData.CreatePasswordHash(), TestData.Now);
        _users.Seed(user);

        var result = await CreateHandler().HandleAsync(new ActivateUserCommand(user.Id.Value));

        result.IsSuccess.Should().BeTrue();
        user.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task Should_Fail_WhenNoCredential()
    {
        User user = User.Register(UserId.New(), TestData.CreateEmail(), TestData.CreateName(), TestData.Now).Value;
        _users.Seed(user);

        var result = await CreateHandler().HandleAsync(new ActivateUserCommand(user.Id.Value));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.CredentialRequired");
    }
}
