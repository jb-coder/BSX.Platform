using System.Net;
using System.Net.Http.Json;
using BSX.Modules.Sales.Application.Dtos;

namespace BSX.Web.IntegrationTests;

public sealed class SalesSecurityEndpointsTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public SalesSecurityEndpointsTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Anonymous_Should_BeDenied()
    {
        HttpResponseMessage leads = await _factory.CreateClient().GetAsync("/sales/leads");
        HttpResponseMessage opportunities = await _factory.CreateClient().GetAsync("/sales/opportunities");

        leads.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        opportunities.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RestrictedUser_Should_BeForbidden()
    {
        HttpClient client = await _factory.CreateAuthenticatedClientAsync(
            IdentityApiFactory.RestrictedEmail,
            IdentityApiFactory.AdminPassword,
            _factory.TenantId);

        HttpResponseMessage read = await client.GetAsync("/sales/leads");
        HttpResponseMessage create = await client.PostAsJsonAsync(
            "/sales/leads",
            new { title = SalesTestData.NewTitle(), customerId = (Guid?)null, organizationId = (Guid?)null, notes = (string?)null });

        read.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        create.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AuthenticatedUserWithoutTenantOrPermission_Should_BeForbidden()
    {
        HttpClient client = await _factory.CreateAuthenticatedClientAsync(
            IdentityApiFactory.InviteeEmail,
            IdentityApiFactory.AdminPassword);

        HttpResponseMessage response = await client.GetAsync("/sales/leads");

        // Fail closed: a membership-less user has no tenant and no sales permission.
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TenantIsolation_Should_NotLeakLeadsAcrossTenants()
    {
        HttpClient tenantA = await _factory.CreateAuthenticatedClientAsync(
            IdentityApiFactory.AdminEmail,
            IdentityApiFactory.AdminPassword);
        HttpClient tenantB = await _factory.CreateAuthenticatedClientAsync(
            IdentityApiFactory.TenantAdminEmail,
            IdentityApiFactory.AdminPassword,
            _factory.OtherTenantId);

        string title = SalesTestData.NewTitle();
        Guid leadA = await SalesTestData.CreateLeadAsync(tenantA, title);

        HttpResponseMessage list = await tenantB.GetAsync($"/sales/leads?search={title}");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResult<LeadSummaryDto>? page =
            await list.Content.ReadFromJsonAsync<PagedResult<LeadSummaryDto>>();
        page!.Items.Should().BeEmpty();

        HttpResponseMessage crossTenant = await tenantB.GetAsync($"/sales/leads/{leadA}");
        crossTenant.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CrossTenantAccess_Should_BeDenied_ForBothDirections()
    {
        HttpClient tenantA = await _factory.CreateAuthenticatedClientAsync(
            IdentityApiFactory.AdminEmail,
            IdentityApiFactory.AdminPassword);
        HttpClient tenantB = await _factory.CreateAuthenticatedClientAsync(
            IdentityApiFactory.TenantAdminEmail,
            IdentityApiFactory.AdminPassword,
            _factory.OtherTenantId);

        Guid leadB = await SalesTestData.CreateLeadAsync(tenantB, SalesTestData.NewTitle());

        HttpResponseMessage response = await tenantA.GetAsync($"/sales/leads/{leadB}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
