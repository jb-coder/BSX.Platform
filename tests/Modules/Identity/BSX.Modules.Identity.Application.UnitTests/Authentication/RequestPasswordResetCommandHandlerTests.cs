using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Application.Authentication.RequestPasswordReset;
using BSX.Modules.Identity.Application.UnitTests.Fakes;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.UnitTests.Authentication;

public sealed class RequestPasswordResetCommandHandlerTests
{
    private readonly InMemoryUserRepository _users = new();
    private readonly FakePasswordResetNotifier _notifier = new();
    private readonly FakePasswordResetTokenHasher _tokenHasher = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly MutableTimeProvider _timeProvider = new(TestData.Now);

    private RequestPasswordResetCommandHandler CreateHandler() => new(
        _users,
        _tokenHasher,
        new FakeSecureTokenGenerator(),
        _notifier,
        new FakeRateLimiter(),
        new RateLimitOptions(),
        _unitOfWork,
        _timeProvider);

    private User SeedUser()
    {
        User user = User.Register(UserId.New(), TestData.CreateEmail(), TestData.CreateName(), TestData.Now).Value;
        user.SetPassword(TestData.CreatePasswordHash(), TestData.Now);
        user.Activate(TestData.Now);
        _users.Seed(user);
        return user;
    }

    [Fact]
    public async Task Should_GenerateToken_AndPersistHashOnly()
    {
        User user = SeedUser();

        var result = await CreateHandler().HandleAsync(new RequestPasswordResetCommand("user@example.com"));

        result.IsSuccess.Should().BeTrue();
        user.PasswordResetToken.Should().NotBeNull();
        _notifier.Notifications.Should().ContainSingle();

        string plaintext = _notifier.Notifications[0].Token;
        user.PasswordResetToken!.Hash.Value.Should().Be(_tokenHasher.Hash(plaintext).Value);
        user.PasswordResetToken.ExpiresOnUtc.Should().Be(TestData.Now.AddMinutes(30));
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_BeEnumerationSafe_WhenUserUnknown()
    {
        var result = await CreateHandler().HandleAsync(new RequestPasswordResetCommand("missing@example.com"));

        result.IsSuccess.Should().BeTrue();
        _notifier.Notifications.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_BeEnumerationSafe_WhenEmailInvalid()
    {
        var result = await CreateHandler().HandleAsync(new RequestPasswordResetCommand("not-an-email"));

        result.IsSuccess.Should().BeTrue();
        _notifier.Notifications.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_RevokePreviousToken()
    {
        User user = SeedUser();
        RequestPasswordResetCommandHandler handler = CreateHandler();
        await handler.HandleAsync(new RequestPasswordResetCommand("user@example.com"));
        var firstHash = user.PasswordResetToken!.Hash.Value;

        await handler.HandleAsync(new RequestPasswordResetCommand("user@example.com"));

        user.PasswordResetToken!.Hash.Value.Should().NotBe(firstHash);
    }

    [Fact]
    public async Task Should_FailExplicitly_WhenDeliveryDisabled()
    {
        SeedUser();
        _notifier.IsDeliveryEnabled = false;

        var result = await CreateHandler().HandleAsync(new RequestPasswordResetCommand("user@example.com"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.PasswordReset.Unavailable");
        _notifier.Notifications.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_FailExplicitly_WhenDeliveryDisabled_EvenForUnknownUser()
    {
        _notifier.IsDeliveryEnabled = false;

        // Enumeration-safe: the same explicit failure for unknown accounts.
        var result = await CreateHandler().HandleAsync(new RequestPasswordResetCommand("missing@example.com"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.PasswordReset.Unavailable");
    }
}
