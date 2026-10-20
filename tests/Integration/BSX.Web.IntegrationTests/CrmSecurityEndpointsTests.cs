using System.Net;
using System.Net.Http.Json;
using BSX.Modules.Crm.Application.Dtos;

namespace BSX.Web.IntegrationTests;

public sealed class CrmSecurityEndpointsTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public CrmSecurityEndpointsTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Anonymous_Should_BeDenied()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync("/crm/customers");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RestrictedUser_Should_BeForbidden()
    {
        HttpClient client = await _factory.CreateAuthenticatedClientAsync(
            IdentityApiFactory.RestrictedEmail,
            IdentityApiFactory.AdminPassword,
            _factory.TenantId);

        HttpResponseMessage response = await client.GetAsync("/crm/customers");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TenantIsolation_Should_NotLeakCustomersAcrossTenants()
    {
        HttpClient tenantA = await _factory.CreateAuthenticatedClientAsync(
            IdentityApiFactory.AdminEmail,
            IdentityApiFactory.AdminPassword);
        HttpClient tenantB = await _factory.CreateAuthenticatedClientAsync(
            IdentityApiFactory.TenantAdminEmail,
            IdentityApiFactory.AdminPassword,
            _factory.OtherTenantId);

        string code = CrmTestData.NewCode();
        Guid customerA = await CrmTestData.CreateCustomerAsync(tenantA, code);

        HttpResponseMessage list = await tenantB.GetAsync($"/crm/customers?search={code}");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResult<CustomerSummaryDto>? page = await list.Content.ReadFromJsonAsync<PagedResult<CustomerSummaryDto>>();
        page!.Items.Should().BeEmpty();

        HttpResponseMessage crossTenant = await tenantB.GetAsync($"/crm/customers/{customerA}");
        crossTenant.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // The same code is valid in a different tenant (tenant-scoped uniqueness).
        Guid customerB = await CrmTestData.CreateCustomerAsync(tenantB, code);
        customerB.Should().NotBe(customerA);
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

        Guid customerB = await CrmTestData.CreateCustomerAsync(tenantB, CrmTestData.NewCode());

        HttpResponseMessage response = await tenantA.GetAsync($"/crm/customers/{customerB}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
