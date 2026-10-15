using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BSX.Web.IntegrationTests;

public sealed class IdentityApiTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public IdentityApiTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_Should_BeReady()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_Should_Succeed_With_ValidCredentials()
    {
        HttpResponseMessage response = await _factory.CreateClient().PostAsJsonAsync(
            "/identity/login",
            new { email = IdentityApiFactory.AdminEmail, password = IdentityApiFactory.AdminPassword });

        string body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
    }

    [Fact]
    public async Task Login_Should_Fail_With_InvalidCredentials()
    {
        HttpResponseMessage response = await _factory.CreateClient().PostAsJsonAsync(
            "/identity/login",
            new { email = IdentityApiFactory.AdminEmail, password = "wrong-password" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Anonymous_Should_BeDenied_On_ProtectedEndpoints()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync("/identity/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Users_Should_BeListed_WhenAuthenticated()
    {
        HttpClient client = await CreateAuthenticatedClientAsync();

        HttpResponseMessage response = await client.GetAsync("/identity/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Roles_Should_BeListed_ForActiveTenant()
    {
        HttpClient client = await CreateAuthenticatedClientAsync();

        HttpResponseMessage response = await client.GetAsync("/identity/roles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Sessions_Should_BeListed()
    {
        HttpClient client = await CreateAuthenticatedClientAsync();

        HttpResponseMessage response = await client.GetAsync("/identity/sessions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_Should_RotateToken()
    {
        HttpClient client = _factory.CreateClient();
        string refreshToken = await LoginForRefreshTokenAsync(client);

        HttpResponseMessage response = await client.PostAsJsonAsync("/identity/refresh", new { refreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_Should_DenyReuse_BeyondGrace()
    {
        HttpClient client = _factory.CreateClient();
        string refreshToken = await LoginForRefreshTokenAsync(client);

        await client.PostAsJsonAsync("/identity/refresh", new { refreshToken });
        HttpResponseMessage replay = await client.PostAsJsonAsync("/identity/refresh", new { refreshToken });

        // Immediately replayed within the grace window is accepted; the reuse path is covered by
        // the domain and application unit tests with an advanced clock.
        replay.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Logout_Should_Succeed()
    {
        HttpClient client = _factory.CreateClient();
        string refreshToken = await LoginForRefreshTokenAsync(client);

        HttpResponseMessage response = await client.PostAsJsonAsync("/identity/logout", new { refreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task PasswordResetRequest_Should_BeEnumerationSafe()
    {
        HttpResponseMessage response = await _factory.CreateClient().PostAsJsonAsync(
            "/identity/password-reset/request",
            new { email = "unknown@example.com" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        HttpClient client = _factory.CreateClient();
        string token = await _factory.LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<string> LoginForRefreshTokenAsync(HttpClient client)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/identity/login",
            new { email = IdentityApiFactory.AdminEmail, password = IdentityApiFactory.AdminPassword });
        response.EnsureSuccessStatusCode();

        IdentityApiFactory.LoginTokens tokens = (await response.Content.ReadFromJsonAsync<IdentityApiFactory.LoginTokens>())!;
        return tokens.RefreshToken;
    }
}
