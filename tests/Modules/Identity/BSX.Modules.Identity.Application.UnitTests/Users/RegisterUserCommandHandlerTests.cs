using BSX.Modules.Identity.Application.UnitTests.Fakes;
using BSX.Modules.Identity.Application.Users.RegisterUser;
using BSX.Modules.Identity.Domain.Enums;

namespace BSX.Modules.Identity.Application.UnitTests.Users;

public sealed class RegisterUserCommandHandlerTests
{
    private readonly InMemoryUserRepository _users = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly TestTimeProvider _timeProvider = new(TestData.Now);

    private RegisterUserCommandHandler CreateHandler()
        => new(_users, new FakePasswordHasher(), _unitOfWork, _timeProvider);

    [Fact]
    public async Task Should_RegisterUser_AndPersist()
    {
        var result = await CreateHandler().HandleAsync(new RegisterUserCommand("user@example.com", "Test User", "Passw0rd!"));

        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().NotBeEmpty();
        _users.Users.Should().ContainSingle();
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_SetPassword_WhenProvided()
    {
        await CreateHandler().HandleAsync(new RegisterUserCommand("user@example.com", "Test User", "Passw0rd!"));

        _users.Users.Single().HasCredential(CredentialType.Password).Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_WhenEmailAlreadyExists()
    {
        _users.Seed(TestData.CreateActiveUser("taken@example.com"));

        var result = await CreateHandler().HandleAsync(new RegisterUserCommand("taken@example.com", "Test User", null));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.EmailAlreadyExists");
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_Fail_WhenEmailInvalid()
    {
        var result = await CreateHandler().HandleAsync(new RegisterUserCommand("not-an-email", "Test User", null));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.Email.Invalid");
    }
}
