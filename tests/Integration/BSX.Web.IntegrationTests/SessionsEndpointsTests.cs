using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BSX.Modules.Identity.Application.Dtos;

namespace BSX.Web.IntegrationTests;

/// <summary>End-to-end coverage for the session endpoints (list, revoke, logout-all).</summary>
public sealed class SessionsEndpointsTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public SessionsEndpointsTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task LogoutAll_Should_RevokeEverySession_WhenAuthenticated()
    {
        HttpClient client = _factory.CreateClient();
        IdentityApiFactory.LoginTokens tokens = await _factory.LoginAsync(
            client,
            IdentityApiFactory.AdminEmail,
            IdentityApiFactory.AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        HttpResponseMessage logoutAll = await client.PostAsync("/identity/logout-all", content: null);

        logoutAll.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage refresh = await client.PostAsJsonAsync(
            "/identity/refresh",
            new { refreshToken = tokens.RefreshToken });

        // The revoked session can no longer be rotated (fail-closed).
        refresh.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task RevokeSession_Should_RevokeTargetSession_WhenOwner()
    {
        HttpClient client = await _factory.CreateAuthenticatedClientAsync(
            IdentityApiFactory.AdminEmail,
            IdentityApiFactory.AdminPassword);

        HttpResponseMessage list = await client.GetAsync("/identity/sessions");
        list.StatusCode.Should().Be(HttpStatusCode.OK);

        List<SessionDto>? sessions = await list.Content.ReadFromJsonAsync<List<SessionDto>>();
        sessions.Should().NotBeNullOrEmpty();

        HttpResponseMessage revoke = await client.DeleteAsync($"/identity/sessions/{sessions![0].Id}");

        revoke.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RevokeSession_Should_ReturnNotFound_WhenUnknown()
    {
        HttpClient client = await _factory.CreateAuthenticatedClientAsync(
            IdentityApiFactory.AdminEmail,
            IdentityApiFactory.AdminPassword);

        HttpResponseMessage revoke = await client.DeleteAsync($"/identity/sessions/{Guid.NewGuid()}");

        revoke.StatusCode.Should().Be(HttpStatusCode.NotFound);
        revoke.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }
}
