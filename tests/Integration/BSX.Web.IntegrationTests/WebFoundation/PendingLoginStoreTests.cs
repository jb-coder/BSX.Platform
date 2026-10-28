using BSX.Web.Authentication;

namespace BSX.Web.IntegrationTests.WebFoundation;

public sealed class PendingLoginStoreTests
{
    [Fact]
    public void SaveAndLoad_ShouldReturnPendingLogin_ByOpaqueToken()
    {
        var store = new PendingLoginStore();
        var pending = new PendingLogin(
            "user@example.com",
            "secret",
            "/app/crm/customers",
            [new PendingTenant(Guid.NewGuid(), DateTimeOffset.UtcNow)],
            DateTimeOffset.UtcNow.AddMinutes(5));

        string token = store.Save(pending);

        token.Should().NotBeNullOrWhiteSpace();
        store.Load(token).Should().Be(pending);
    }

    [Fact]
    public void Load_ShouldReturnNull_WhenTokenIsUnknown()
    {
        var store = new PendingLoginStore();

        store.Load("unknown").Should().BeNull();
    }

    [Fact]
    public void Load_ShouldReturnNull_AndRemoveExpiredEntry()
    {
        var store = new PendingLoginStore();
        string token = store.Save(new PendingLogin(
            "user@example.com",
            "secret",
            null,
            [new PendingTenant(Guid.NewGuid(), null)],
            DateTimeOffset.UtcNow.AddMinutes(-1)));

        store.Load(token).Should().BeNull();
        store.Load(token).Should().BeNull();
    }

    [Fact]
    public void Remove_ShouldForgetPendingLogin()
    {
        var store = new PendingLoginStore();
        string token = store.Save(new PendingLogin(
            "user@example.com",
            "secret",
            null,
            [],
            DateTimeOffset.UtcNow.AddMinutes(5)));

        store.Remove(token);

        store.Load(token).Should().BeNull();
    }
}
