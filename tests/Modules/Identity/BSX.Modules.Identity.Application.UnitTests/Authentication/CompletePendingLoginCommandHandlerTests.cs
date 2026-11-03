using BSX.Modules.Identity.Application.Authentication;
using BSX.Modules.Identity.Application.Authentication.CompletePendingLogin;
using BSX.Modules.Identity.Application.UnitTests.Fakes;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.UnitTests.Authentication;

public sealed class CompletePendingLoginCommandHandlerTests
{
    private readonly InMemoryUserRepository _users = new();
    private readonly InMemoryUserSessionRepository _sessions = new();
    private readonly InMemoryTenantMembershipRepository _memberships = new();
    private readonly InMemoryRoleRepository _roles = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly MutableTimeProvider _timeProvider = new(TestData.Now);
    private readonly FakePendingLoginStore _pendingLogins = new();

    private CompletePendingLoginCommandHandler CreateHandler() => new(
        _pendingLogins,
        _users,
        _memberships,
        new AuthenticationSessionIssuer(
            _sessions,
            new PermissionResolver(_roles, TestPermissionCatalog.Platform),
            new FakeSecureTokenGenerator(),
            new FakeRefreshTokenHasher(),
            new FakeAccessTokenIssuer(),
            _unitOfWork,
            _timeProvider));

    [Fact]
    public async Task Should_IssueTokens_ForTheVerifiedUserAndSelectedTenant()
    {
        User user = TestData.CreateActiveUser();
        _users.Seed(user);
        TenantId tenantId = TenantId.New();
        _memberships.Seed(TestData.CreateActiveMembership(tenantId, user.Id));

        PendingLoginHandle handle = _pendingLogins.Seed(CreatePendingLogin(user, tenantId));

        var result = await CreateHandler().HandleAsync(new CompletePendingLoginCommand(
            handle.SecretToken,
            handle.SelectionId,
            tenantId.Value,
            "ua",
            "127.0.0.1"));

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Value.RefreshToken.Should().NotBeNullOrWhiteSpace();
        _sessions.Sessions.Should().ContainSingle();
        _sessions.Sessions.Single().TenantId.Should().Be(tenantId);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Reject_AnExpiredOrConsumedPendingLogin()
    {
        User user = TestData.CreateActiveUser();
        _users.Seed(user);
        TenantId tenantId = TenantId.New();
        _memberships.Seed(TestData.CreateActiveMembership(tenantId, user.Id));
        PendingLoginHandle handle = _pendingLogins.Seed(CreatePendingLogin(user, tenantId));

        var handler = CreateHandler();
        (await handler.HandleAsync(new CompletePendingLoginCommand(
            handle.SecretToken, handle.SelectionId, tenantId.Value, null, null))).IsSuccess.Should().BeTrue();

        var replay = await handler.HandleAsync(new CompletePendingLoginCommand(
            handle.SecretToken, handle.SelectionId, tenantId.Value, null, null));

        replay.IsFailure.Should().BeTrue();
        replay.Error.Code.Should().Be("Identity.Authentication.PendingLoginExpired");
    }

    [Fact]
    public async Task Should_Reject_A_MismatchedSelectionHandle()
    {
        User user = TestData.CreateActiveUser();
        _users.Seed(user);
        TenantId tenantId = TenantId.New();
        _memberships.Seed(TestData.CreateActiveMembership(tenantId, user.Id));
        PendingLoginHandle handle = _pendingLogins.Seed(CreatePendingLogin(user, tenantId));

        var result = await CreateHandler().HandleAsync(new CompletePendingLoginCommand(
            handle.SecretToken,
            "a-different-selection",
            tenantId.Value,
            null,
            null));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Authentication.PendingLoginExpired");
    }

    [Fact]
    public async Task Should_Reject_A_TenantThatWasNotInTheVerifiedOptions()
    {
        User user = TestData.CreateActiveUser();
        _users.Seed(user);
        TenantId allowedTenant = TenantId.New();
        TenantId otherTenant = TenantId.New();
        _memberships.Seed(TestData.CreateActiveMembership(allowedTenant, user.Id));
        _memberships.Seed(TestData.CreateActiveMembership(otherTenant, user.Id));
        PendingLoginHandle handle = _pendingLogins.Seed(CreatePendingLogin(user, allowedTenant));

        var result = await CreateHandler().HandleAsync(new CompletePendingLoginCommand(
            handle.SecretToken,
            handle.SelectionId,
            otherTenant.Value,
            null,
            null));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Authentication.InvalidTenant");
        _sessions.Sessions.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_FailClosed_WhenTheUserIsNoLongerActive()
    {
        User user = TestData.CreateActiveUser();
        _users.Seed(user);
        TenantId tenantId = TenantId.New();
        _memberships.Seed(TestData.CreateActiveMembership(tenantId, user.Id));
        PendingLoginHandle handle = _pendingLogins.Seed(CreatePendingLogin(user, tenantId));
        user.Deactivate();

        var result = await CreateHandler().HandleAsync(new CompletePendingLoginCommand(
            handle.SecretToken,
            handle.SelectionId,
            tenantId.Value,
            null,
            null));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Authentication.InvalidCredentials");
        _sessions.Sessions.Should().BeEmpty();
    }

    private static PendingLogin CreatePendingLogin(User user, TenantId tenantId) => new(
        user.Id.Value,
        user.Email.Value,
        "/app",
        [new PendingTenant(tenantId.Value, TestData.Now)],
        TestData.Now.AddMinutes(5));
}
