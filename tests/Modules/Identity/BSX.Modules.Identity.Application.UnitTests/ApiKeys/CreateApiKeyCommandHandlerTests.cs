using BSX.Modules.Identity.Application.ApiKeys.CreateApiKey;
using BSX.Modules.Identity.Application.UnitTests.Fakes;
using BSX.Modules.Identity.Domain.ServiceAccounts;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.UnitTests.ApiKeys;

public sealed class CreateApiKeyCommandHandlerTests
{
    private readonly InMemoryServiceAccountRepository _accounts = new();
    private readonly InMemoryApiKeyRepository _apiKeys = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly TestTimeProvider _timeProvider = new(TestData.Now);

    private CreateApiKeyCommandHandler CreateHandler() => new(
        _accounts,
        _apiKeys,
        new FakeSecureTokenGenerator(),
        new FakeApiKeyHasher(),
        _unitOfWork,
        _timeProvider);

    [Fact]
    public async Task Should_CreateApiKey_AndReturnSecretOnce()
    {
        ServiceAccount account = TestData.CreateServiceAccount(TenantId.From(Guid.NewGuid()), "billing-bot");
        _accounts.Seed(account);

        var result = await CreateHandler().HandleAsync(new CreateApiKeyCommand(account.Id.Value, "deploy", null, ["crm.customers.read"]));

        result.IsSuccess.Should().BeTrue();
        result.Value.Secret.Should().NotBeNullOrWhiteSpace();
        result.Value.Prefix.Should().HaveLength(8);
        _apiKeys.Keys.Should().ContainSingle();
        _apiKeys.Keys.Single().Scopes.Should().ContainSingle();
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_WhenServiceAccountNotFound()
    {
        var result = await CreateHandler().HandleAsync(new CreateApiKeyCommand(Guid.NewGuid(), "deploy", null, null));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.ServiceAccount.NotFound");
    }
}
