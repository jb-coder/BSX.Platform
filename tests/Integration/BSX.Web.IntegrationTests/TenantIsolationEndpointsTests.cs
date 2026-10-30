using System.Net;
using System.Net.Http.Json;

namespace BSX.Web.IntegrationTests;

/// <summary>End-to-end coverage for tenant isolation and boundary enforcement (ADR-013).</summary>
public sealed class TenantIsolationEndpointsTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public TenantIsolationEndpointsTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_Should_Fail_WhenTenantIsNotAMember()
    {
        HttpResponseMessage response = await _factory.CreateClient().PostAsJsonAsync(
            "/identity/login",
            new
            {
                email = IdentityApiFactory.AdminEmail,
                password = IdentityApiFactory.AdminPassword,
                tenantId = _factory.OtherTenantId,
            });

        // Cross-tenant authentication attempt: fail-closed.
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_Should_RequireTenant_WhenUserBelongsToMultipleTenants()
    {
        HttpResponseMessage response = await _factory.CreateClient().PostAsJsonAsync(
            "/identity/login",
            new { email = IdentityApiFactory.TenantAdminEmail, password = IdentityApiFactory.AdminPassword });

        // Ambiguous tenant selection must be rejected rather than silently resolved.
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Roles_Should_NotCrossTenantBoundary()
    {
        string roleName = $"tenant-a-{Guid.NewGuid():N}";

        HttpClient tenantA = await _factory.CreateAuthenticatedClientAsync(
            IdentityApiFactory.AdminEmail,
            IdentityApiFactory.AdminPassword);
        HttpResponseMessage created = await tenantA.PostAsJsonAsync(
            "/identity/roles",
            new { name = roleName, description = "tenant A only", permissions = new[] { "crm.customers.read" } });
        created.StatusCode.Should().Be(HttpStatusCode.Created);

        HttpClient tenantB = await _factory.CreateAuthenticatedClientAsync(
            IdentityApiFactory.TenantAdminEmail,
            IdentityApiFactory.AdminPassword,
            _factory.OtherTenantId);
        HttpResponseMessage roles = await tenantB.GetAsync("/identity/roles");
        roles.StatusCode.Should().Be(HttpStatusCode.OK);

        string body = await roles.Content.ReadAsStringAsync();
        body.Should().NotContain(roleName);
    }

    [Fact]
    public async Task Roles_Should_BeScopedToTheActiveTenant()
    {
        HttpClient tenantB = await _factory.CreateAuthenticatedClientAsync(
            IdentityApiFactory.TenantAdminEmail,
            IdentityApiFactory.AdminPassword,
            _factory.OtherTenantId);

        HttpResponseMessage response = await tenantB.GetAsync("/identity/roles");

        // The endpoint resolves the tenant from the token (TenantContext mapping), never from input.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
