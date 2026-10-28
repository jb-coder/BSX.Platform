using System.Net;
using System.Net.Http.Json;
using BSX.Modules.Identity.Application.Dtos;

namespace BSX.Web.IntegrationTests.WebFoundation;

public sealed class TenantDiscoveryEndpointTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public TenantDiscoveryEndpointTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Discover_ShouldReturnActiveTenants_AfterCredentialValidation()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/identity/login/tenants",
            new
            {
                email = IdentityApiFactory.TenantAdminEmail,
                password = IdentityApiFactory.AdminPassword,
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        IReadOnlyList<TenantOptionDto>? options = await response.Content
            .ReadFromJsonAsync<IReadOnlyList<TenantOptionDto>>();

        options.Should().NotBeNull();
        options!.Select(option => option.TenantId)
            .Should().BeEquivalentTo([_factory.TenantId, _factory.OtherTenantId]);
    }

    [Fact]
    public async Task Discover_ShouldReturnUnauthorized_WhenCredentialsAreInvalid()
    {
        HttpResponseMessage response = await _factory.CreateClient().PostAsJsonAsync(
            "/identity/login/tenants",
            new { email = IdentityApiFactory.AdminEmail, password = "not-the-password" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
