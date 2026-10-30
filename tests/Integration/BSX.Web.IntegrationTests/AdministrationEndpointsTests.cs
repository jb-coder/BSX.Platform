using System.Net;
using System.Net.Http.Json;

namespace BSX.Web.IntegrationTests;

/// <summary>End-to-end coverage for the administration endpoints (create user, role, membership).</summary>
public sealed class AdministrationEndpointsTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public AdministrationEndpointsTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task CreateUser_Should_Succeed_WhenAuthorized()
    {
        HttpClient client = await _factory.CreateAuthenticatedClientAsync(
            IdentityApiFactory.AdminEmail,
            IdentityApiFactory.AdminPassword);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/identity/users",
            new
            {
                email = $"new-{Guid.NewGuid():N}@example.com",
                name = "New User",
                password = IdentityApiFactory.AdminPassword,
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateRole_Should_Succeed_WhenAuthorized()
    {
        HttpClient client = await _factory.CreateAuthenticatedClientAsync(
            IdentityApiFactory.AdminEmail,
            IdentityApiFactory.AdminPassword);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/identity/roles",
            new
            {
                name = $"role-{Guid.NewGuid():N}",
                description = "integration",
                permissions = new[] { "crm.customers.read" },
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateMembership_Should_Succeed_WhenAuthorized()
    {
        HttpClient client = await _factory.CreateAuthenticatedClientAsync(
            IdentityApiFactory.AdminEmail,
            IdentityApiFactory.AdminPassword);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/identity/memberships",
            new { userId = _factory.InviteeUserId, invitedByUserId = _factory.AdminUserId });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
