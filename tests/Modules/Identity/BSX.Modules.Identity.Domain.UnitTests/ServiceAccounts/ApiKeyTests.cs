using BSX.Modules.Identity.Domain.DomainEvents;
using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.ServiceAccounts;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Domain.UnitTests.ServiceAccounts;

public sealed class ApiKeyTests
{
    private static ApiKey CreateKey(DateTimeOffset? expiresOnUtc = null)
        => ApiKey.Create(
            ApiKeyId.New(),
            PrincipalId.New(),
            PrincipalType.ServiceAccount,
            "deploy-key",
            ApiKeyPrefix.From("bsx_live"),
            ApiKeyHash.From("hashed"),
            TestData.Now,
            expiresOnUtc,
            [TestData.CreatePermission("crm.customers.read")]).Value;

    [Fact]
    public void Should_Create_Active_AndRaiseEvent()
    {
        var key = CreateKey();

        key.IsActiveAt(TestData.Now).Should().BeTrue();
        key.Scopes.Should().ContainSingle();
        key.DomainEvents.Should().ContainSingle(domainEvent => domainEvent is ApiKeyCreated);
    }

    [Fact]
    public void Should_Fail_WhenExpiryInPast()
    {
        Result<ApiKey> result = ApiKey.Create(
            ApiKeyId.New(),
            PrincipalId.New(),
            PrincipalType.ServiceAccount,
            "deploy-key",
            ApiKeyPrefix.From("bsx_live"),
            ApiKeyHash.From("hashed"),
            TestData.Now,
            TestData.Now.AddMinutes(-1));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.ApiKey.ExpiryInvalid");
    }

    [Fact]
    public void Should_Revoke_AndRaiseEvent()
    {
        var key = CreateKey();

        key.Revoke(TestData.Now).IsSuccess.Should().BeTrue();

        key.IsRevoked.Should().BeTrue();
        key.IsActiveAt(TestData.Now).Should().BeFalse();
        key.DomainEvents.Should().Contain(domainEvent => domainEvent is ApiKeyRevoked);
    }

    [Fact]
    public void Should_Fail_WhenRevokedTwice()
    {
        var key = CreateKey();
        key.Revoke(TestData.Now);

        Result result = key.Revoke(TestData.Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.ApiKey.AlreadyRevoked");
    }

    [Fact]
    public void Should_Fail_MarkUsed_WhenRevoked()
    {
        var key = CreateKey();
        key.Revoke(TestData.Now);

        Result result = key.MarkUsed(TestData.Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.ApiKey.Revoked");
    }

    [Fact]
    public void Should_NotBeActive_AfterExpiry()
    {
        var key = CreateKey(TestData.Now.AddMinutes(10));

        key.IsActiveAt(TestData.Now.AddMinutes(11)).Should().BeFalse();
        key.IsExpiredAt(TestData.Now.AddMinutes(11)).Should().BeTrue();
    }
}
