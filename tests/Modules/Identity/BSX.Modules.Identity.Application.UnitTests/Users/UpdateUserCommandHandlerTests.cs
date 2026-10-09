using BSX.Modules.Identity.Application.UnitTests.Fakes;
using BSX.Modules.Identity.Application.Users.UpdateUser;
using BSX.Modules.Identity.Domain.Users;

namespace BSX.Modules.Identity.Application.UnitTests.Users;

public sealed class UpdateUserCommandHandlerTests
{
    private readonly InMemoryUserRepository _users = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private UpdateUserCommandHandler CreateHandler() => new(_users, _unitOfWork);

    [Fact]
    public async Task Should_Fail_WhenUserNotFound()
    {
        var result = await CreateHandler().HandleAsync(new UpdateUserCommand(Guid.NewGuid(), "New Name", "new@example.com"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.NotFound");
    }

    [Fact]
    public async Task Should_UpdateNameAndEmail()
    {
        User user = TestData.CreateActiveUser("old@example.com");
        _users.Seed(user);

        var result = await CreateHandler().HandleAsync(new UpdateUserCommand(user.Id.Value, "New Name", "new@example.com"));

        result.IsSuccess.Should().BeTrue();
        user.Name.Value.Should().Be("New Name");
        user.Email.Value.Should().Be("new@example.com");
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_WhenNewEmailAlreadyTaken()
    {
        User first = TestData.CreateActiveUser("first@example.com");
        User second = TestData.CreateActiveUser("second@example.com");
        _users.Seed(first, second);

        var result = await CreateHandler().HandleAsync(new UpdateUserCommand(second.Id.Value, "Second", "first@example.com"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.EmailAlreadyExists");
    }

    [Fact]
    public async Task Should_Fail_WhenUserIdIsEmpty()
    {
        var result = await CreateHandler().HandleAsync(new UpdateUserCommand(Guid.Empty, "Name", "user@example.com"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.InvalidId");
    }
}
