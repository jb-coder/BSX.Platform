using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Application.Authentication;
using BSX.Modules.Identity.Application.Authentication.DiscoverTenantOptions;
using BSX.Modules.Identity.Application.UnitTests.Fakes;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.UnitTests.Authentication;

public sealed class DiscoverTenantOptionsCommandHandlerTests
{
    private readonly InMemoryUserRepository _users = new();
    private readonly InMemoryTenantMembershipRepository _memberships = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly MutableTimeProvider _timeProvider = new(TestData.Now);

    private DiscoverTenantOptionsCommandHandler CreateHandler() => new(
        new CredentialAuthenticator(_users, _passwordHasher, _unitOfWork, _timeProvider),
        _memberships,
        new FakeRateLimiter(),
        new RateLimitOptions());

    [Fact]
    public async Task Should_ReturnActiveTenants_ForValidCredentials()
    {
        User user = SeedUser();
        TenantId first = TenantId.New();
        TenantId second = TenantId.New();
        _memberships.Seed(
            TestData.CreateActiveMembership(first, user.Id),
            TestData.CreateActiveMembership(second, user.Id));

        var result = await CreateHandler().HandleAsync(
            new DiscoverTenantOptionsCommand("user@example.com", "Passw0rd!"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(option => option.TenantId).Should().BeEquivalentTo(new[] { first.Value, second.Value });
    }

    [Fact]
    public async Task Should_ReturnEmptyOptions_WhenUserHasNoMemberships()
    {
        SeedUser();

        var result = await CreateHandler().HandleAsync(
            new DiscoverTenantOptionsCommand("user@example.com", "Passw0rd!"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_ReturnInvalidCredentials_WhenPasswordIsWrong()
    {
        SeedUser();

        var result = await CreateHandler().HandleAsync(
            new DiscoverTenantOptionsCommand("user@example.com", "wrong"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Authentication.InvalidCredentials");
    }

    [Fact]
    public async Task Should_RateLimit_AfterThreshold()
    {
        SeedUser();
        var handler = new DiscoverTenantOptionsCommandHandler(
            new CredentialAuthenticator(_users, _passwordHasher, _unitOfWork, _timeProvider),
            _memberships,
            new FakeRateLimiter(maxRequests: 1),
            new RateLimitOptions());

        await handler.HandleAsync(new DiscoverTenantOptionsCommand("user@example.com", "Passw0rd!"));
        var limited = await handler.HandleAsync(new DiscoverTenantOptionsCommand("user@example.com", "Passw0rd!"));

        limited.IsFailure.Should().BeTrue();
        limited.Error.Code.Should().Be("Identity.Authentication.TooManyRequests");
    }

    private User SeedUser()
    {
        User user = User.Register(
            UserId.New(),
            TestData.CreateEmail("user@example.com"),
            TestData.CreateName(),
            TestData.Now).Value;
        user.SetPassword(_passwordHasher.Hash("Passw0rd!"), TestData.Now);
        user.Activate(TestData.Now);
        _users.Seed(user);
        return user;
    }
}
