using BSX.Modules.Identity.Application.Authentication;
using BSX.Web.Authentication;

namespace BSX.Web.IntegrationTests.WebFoundation;

public sealed class PendingLoginStoreTests
{
    [Fact]
    public void SaveAndLoad_ShouldReturnPendingLogin_BySecretToken()
    {
        var store = new PendingLoginStore();
        var pending = new PendingLogin(
            Guid.NewGuid(),
            "user@example.com",
            "/app/crm/customers",
            [new PendingTenant(Guid.NewGuid(), DateTimeOffset.UtcNow)],
            DateTimeOffset.UtcNow.AddMinutes(5));

        PendingLoginHandle handle = store.Save(pending);

        handle.SelectionId.Should().NotBeNullOrWhiteSpace();
        handle.SecretToken.Should().NotBeNullOrWhiteSpace();
        handle.SelectionId.Should().NotBe(handle.SecretToken);
        store.Get(handle.SecretToken).Should().BeEquivalentTo(pending with { SelectionId = handle.SelectionId });
        store.GetBySelectionId(handle.SelectionId).Should().BeEquivalentTo(pending with { SelectionId = handle.SelectionId });
    }

    [Fact]
    public void PendingLogin_ShouldNotContainAPassword()
    {
        typeof(PendingLogin).GetProperty("Password").Should().BeNull(
            "the password is verified once and must never be retained in a pending sign-in");
    }

    [Fact]
    public void SelectionHandle_ShouldNotCompleteASignIn_WithoutTheSecretToken()
    {
        var store = new PendingLoginStore();
        PendingLoginHandle handle = store.Save(new PendingLogin(
            Guid.NewGuid(),
            "user@example.com",
            null,
            [new PendingTenant(Guid.NewGuid(), null)],
            DateTimeOffset.UtcNow.AddMinutes(5)));

        // The URL handle can render the chooser but cannot be consumed as a bearer credential.
        store.GetBySelectionId(handle.SelectionId).Should().NotBeNull();
        store.Consume(handle.SelectionId).Should().BeNull();
    }

    [Fact]
    public void Load_ShouldReturnNull_WhenTokenIsUnknown()
    {
        var store = new PendingLoginStore();

        store.Get("unknown").Should().BeNull();
        store.GetBySelectionId("unknown").Should().BeNull();
    }

    [Fact]
    public void Load_ShouldReturnNull_AndRemoveExpiredEntry()
    {
        var store = new PendingLoginStore();
        PendingLoginHandle handle = store.Save(new PendingLogin(
            Guid.NewGuid(),
            "user@example.com",
            null,
            [new PendingTenant(Guid.NewGuid(), null)],
            DateTimeOffset.UtcNow.AddMinutes(-1)));

        store.Get(handle.SecretToken).Should().BeNull();
        store.GetBySelectionId(handle.SelectionId).Should().BeNull();
    }

    [Fact]
    public void Consume_ShouldBeSingleUse()
    {
        var store = new PendingLoginStore();
        PendingLoginHandle handle = store.Save(new PendingLogin(
            Guid.NewGuid(),
            "user@example.com",
            null,
            [],
            DateTimeOffset.UtcNow.AddMinutes(5)));

        store.Consume(handle.SecretToken).Should().NotBeNull();
        store.Consume(handle.SecretToken).Should().BeNull();
        store.GetBySelectionId(handle.SelectionId).Should().BeNull();
    }

    [Fact]
    public void Remove_ShouldForgetPendingLogin()
    {
        var store = new PendingLoginStore();
        PendingLoginHandle handle = store.Save(new PendingLogin(
            Guid.NewGuid(),
            "user@example.com",
            null,
            [],
            DateTimeOffset.UtcNow.AddMinutes(5)));

        store.Remove(handle.SecretToken);

        store.Get(handle.SecretToken).Should().BeNull();
        store.GetBySelectionId(handle.SelectionId).Should().BeNull();
    }
}
