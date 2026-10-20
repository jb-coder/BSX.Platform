using System.Net;
using System.Net.Http.Json;
using BSX.Modules.Crm.Application.Dtos;

namespace BSX.Web.IntegrationTests;

public sealed class CrmCustomerEndpointsTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public CrmCustomerEndpointsTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Create_Should_Succeed_And_BeReadable()
    {
        HttpClient client = await AdminClientAsync();
        string code = CrmTestData.NewCode();

        HttpResponseMessage create = await client.PostAsJsonAsync(
            "/crm/customers",
            new { code, name = "Acme Corporation", taxIdentifier = "TAX-1" });

        create.StatusCode.Should().Be(HttpStatusCode.Created);
        Guid id = (await create.Content.ReadFromJsonAsync<Guid>())!;
        create.Headers.Location!.ToString().Should().Be($"/crm/customers/{id}");

        CustomerDto customer = await CrmTestData.GetCustomerAsync(client, id);
        customer.Code.Should().Be(code.ToUpperInvariant());
        customer.Name.Should().Be("Acme Corporation");
        customer.TaxIdentifier.Should().Be("TAX-1");
        customer.Status.Should().Be("Active");
    }

    [Fact]
    public async Task Create_Should_ReturnConflict_WhenCodeAlreadyExists()
    {
        HttpClient client = await AdminClientAsync();
        string code = CrmTestData.NewCode();
        await CrmTestData.CreateCustomerAsync(client, code);

        HttpResponseMessage duplicate = await client.PostAsJsonAsync(
            "/crm/customers",
            new { code, name = "Other", taxIdentifier = (string?)null });

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task List_Should_ReturnTheCreatedCustomer()
    {
        HttpClient client = await AdminClientAsync();
        string code = CrmTestData.NewCode();
        Guid id = await CrmTestData.CreateCustomerAsync(client, code);

        HttpResponseMessage response = await client.GetAsync($"/crm/customers?search={code}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResult<CustomerSummaryDto>? page = await response.Content.ReadFromJsonAsync<PagedResult<CustomerSummaryDto>>();
        page!.Items.Should().ContainSingle(item => item.Id == id);
    }

    [Fact]
    public async Task Update_Should_ChangeNameAndTaxIdentifier()
    {
        HttpClient client = await AdminClientAsync();
        Guid id = await CrmTestData.CreateCustomerAsync(client);

        HttpResponseMessage update = await client.PutAsJsonAsync(
            $"/crm/customers/{id}",
            new { name = "Renamed Corporation", taxIdentifier = "TAX-2" });

        update.StatusCode.Should().Be(HttpStatusCode.NoContent);

        CustomerDto customer = await CrmTestData.GetCustomerAsync(client, id);
        customer.Name.Should().Be("Renamed Corporation");
        customer.TaxIdentifier.Should().Be("TAX-2");
    }

    [Fact]
    public async Task ActivateAndDeactivate_Should_ChangeStatus()
    {
        HttpClient client = await AdminClientAsync();
        Guid id = await CrmTestData.CreateCustomerAsync(client);

        HttpResponseMessage deactivate = await client.PostAsync($"/crm/customers/{id}/deactivate", content: null);
        deactivate.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await CrmTestData.GetCustomerAsync(client, id)).Status.Should().Be("Deactivated");

        HttpResponseMessage activate = await client.PostAsync($"/crm/customers/{id}/activate", content: null);
        activate.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await CrmTestData.GetCustomerAsync(client, id)).Status.Should().Be("Active");
    }

    [Fact]
    public async Task AssignAndClearOrganization_Should_UpdateCustomer()
    {
        HttpClient client = await AdminClientAsync();
        Guid customerId = await CrmTestData.CreateCustomerAsync(client);
        Guid organizationId = await CrmTestData.CreateOrganizationAsync(client);

        HttpResponseMessage assign = await client.PostAsJsonAsync(
            $"/crm/customers/{customerId}/organization",
            new { organizationId });
        assign.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await CrmTestData.GetCustomerAsync(client, customerId)).OrganizationId.Should().Be(organizationId);

        HttpResponseMessage clear = await client.DeleteAsync($"/crm/customers/{customerId}/organization");
        clear.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await CrmTestData.GetCustomerAsync(client, customerId)).OrganizationId.Should().BeNull();
    }

    [Fact]
    public async Task Update_Should_ReturnNotFound_WhenCustomerDoesNotExist()
    {
        HttpClient client = await AdminClientAsync();

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/crm/customers/{Guid.NewGuid()}",
            new { name = "Missing", taxIdentifier = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private Task<HttpClient> AdminClientAsync()
        => _factory.CreateAuthenticatedClientAsync(IdentityApiFactory.AdminEmail, IdentityApiFactory.AdminPassword);
}
