using BSX.Modules.Identity.Application.ApiKeys.RevokeApiKey;
using BSX.Modules.Identity.Application.Authentication;
using BSX.Modules.Identity.Application.UnitTests.Fakes;
using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.ServiceAccounts;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.UnitTests.ApiKeys;

public sealed class RevokeApiKeyCommandHandlerTests
{
    private readonly InMemoryServiceAccountRepository _accounts = new();
    private readonly InMemoryApiKeyRepository _apiKeys = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly TestTimeProvider _timeProvider = new(TestData.Now);

    private RevokeApiKeyCommandHandler CreateHandler(TenantId activeTenant) => new(
        _apiKeys,
        _accounts,
        _unitOfWork,
        new TenantGuard(new FakeTenantContext(activeTenant.Value)),
        _timeProvider);

    [Fact]
    public async Task Should_Revoke_WhenKeyBelongsToActiveTenant()
    {
        TenantId tenantId = TenantId.New();
        ServiceAccount account = TestData.CreateServiceAccount(tenantId);
        ApiKey key = CreateKey(account);
        _accounts.Seed(account);
        _apiKeys.Seed(key);

        var result = await CreateHandler(tenantId)
            .HandleAsync(new RevokeApiKeyCommand(key.Id.Value));

        result.IsSuccess.Should().BeTrue();
        key.IsRevoked.Should().BeTrue();
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_FailClosed_WhenKeyBelongsToAnotherTenant()
    {
        ServiceAccount account = TestData.CreateServiceAccount(TenantId.New());
        ApiKey key = CreateKey(account);
        _accounts.Seed(account);
        _apiKeys.Seed(key);

        var result = await CreateHandler(TenantId.New())
            .HandleAsync(new RevokeApiKeyCommand(key.Id.Value));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Authorization.TenantForbidden");
        key.IsRevoked.Should().BeFalse();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_FailClosed_WhenTheOwningServiceAccountIsMissing()
    {
        ServiceAccount account = TestData.CreateServiceAccount(TenantId.New());
        ApiKey key = CreateKey(account);
        _apiKeys.Seed(key);

        var result = await CreateHandler(account.TenantId)
            .HandleAsync(new RevokeApiKeyCommand(key.Id.Value));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.ApiKey.NotFound");
        key.IsRevoked.Should().BeFalse();
    }

    private static ApiKey CreateKey(ServiceAccount account)
        => ApiKey.Create(
            ApiKeyId.New(),
            account.Principal,
            PrincipalType.ServiceAccount,
            "deploy-key",
            ApiKeyPrefix.From("bsx_live"),
            ApiKeyHash.From("hashed"),
            TestData.Now).Value;
}
