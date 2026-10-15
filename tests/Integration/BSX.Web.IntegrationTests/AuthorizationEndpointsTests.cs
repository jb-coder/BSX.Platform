using System.Net;
using System.Net.Http.Json;

namespace BSX.Web.IntegrationTests;

/// <summary>End-to-end coverage for permission enforcement (authentication is not authorization).</summary>
public sealed class AuthorizationEndpointsTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public AuthorizationEndpointsTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task ListUsers_Should_ReturnForbidden_WhenPermissionMissing()
    {
        HttpClient client = await _factory.CreateAuthenticatedClientAsync(
            IdentityApiFactory.RestrictedEmail,
            IdentityApiFactory.AdminPassword,
            _factory.TenantId);

        HttpResponseMessage response = await client.GetAsync("/identity/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task CreateUser_Should_ReturnForbidden_WhenPermissionMissing()
    {
        HttpClient client = await _factory.CreateAuthenticatedClientAsync(
            IdentityApiFactory.RestrictedEmail,
            IdentityApiFactory.AdminPassword,
            _factory.TenantId);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/identity/users",
            new { email = "should-not-exist@example.com", name = "Denied", password = IdentityApiFactory.AdminPassword });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateRole_Should_ReturnForbidden_WhenPermissionMissing()
    {
        HttpClient client = await _factory.CreateAuthenticatedClientAsync(
            IdentityApiFactory.RestrictedEmail,
            IdentityApiFactory.AdminPassword,
            _factory.TenantId);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/identity/roles",
            new { name = "denied-role", description = "denied", permissions = Array.Empty<string>() });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
