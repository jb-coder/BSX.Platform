using BSX.Modules.Identity.Application.UnitTests.Fakes;
using BSX.Modules.Identity.Application.Users.GetUserById;
using BSX.Modules.Identity.Domain.Users;

namespace BSX.Modules.Identity.Application.UnitTests.Users;

public sealed class GetUserByIdQueryHandlerTests
{
    private readonly InMemoryUserRepository _users = new();

    private GetUserByIdQueryHandler CreateHandler() => new(_users);

    [Fact]
    public async Task Should_ReturnUser_WhenFound()
    {
        User user = TestData.CreateActiveUser("user@example.com");
        _users.Seed(user);

        var result = await CreateHandler().HandleAsync(new GetUserByIdQuery(user.Id.Value));

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(user.Id.Value);
        result.Value.Email.Should().Be("user@example.com");
    }

    [Fact]
    public async Task Should_Fail_WhenNotFound()
    {
        var result = await CreateHandler().HandleAsync(new GetUserByIdQuery(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.NotFound");
    }
}
