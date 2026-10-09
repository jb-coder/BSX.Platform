using BSX.Modules.Identity.Application.Dtos;
using BSX.Modules.Identity.Application.UnitTests.Fakes;
using BSX.Modules.Identity.Application.Users.ListUsers;

namespace BSX.Modules.Identity.Application.UnitTests.Users;

public sealed class ListUsersQueryHandlerTests
{
    private readonly InMemoryUserReadRepository _users = new();

    private ListUsersQueryHandler CreateHandler() => new(_users);

    [Fact]
    public async Task Should_ClampPageAndSize()
    {
        for (int index = 0; index < 25; index++)
        {
            _users.Seed(new UserSummaryDto(Guid.NewGuid(), $"user{index}@example.com", "User", "Active"));
        }

        var result = await CreateHandler().HandleAsync(new ListUsersQuery(0, 1000, null));

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(1);
        result.Value.Size.Should().Be(20);
        result.Value.TotalCount.Should().Be(25);
        result.Value.Items.Should().HaveCount(20);
    }

    [Fact]
    public async Task Should_Filter_BySearch()
    {
        _users.Seed(new UserSummaryDto(Guid.NewGuid(), "alice@example.com", "Alice", "Active"));
        _users.Seed(new UserSummaryDto(Guid.NewGuid(), "bob@example.com", "Bob", "Active"));

        var result = await CreateHandler().HandleAsync(new ListUsersQuery(1, 20, "alice"));

        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.Should().ContainSingle();
    }
}
