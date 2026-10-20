using System.Net;
using System.Net.Http.Json;
using BSX.Modules.Crm.Application.Dtos;

namespace BSX.Web.IntegrationTests;

public sealed class CrmOrganizationEndpointsTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public CrmOrganizationEndpointsTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Create_Should_Succeed_And_BeReadable()
    {
        HttpClient client = await AdminClientAsync();
        string name = $"Org-{Guid.NewGuid():N}";

        HttpResponseMessage create = await client.PostAsJsonAsync(
            "/crm/organizations",
            new
            {
                name,
                taxIdentifier = "TAX-9",
                address = new
                {
                    line1 = "5 Corporate Way",
                    line2 = (string?)null,
                    city = "Springfield",
                    region = (string?)null,
                    postalCode = (string?)null,
                    country = "US",
                },
            });

        create.StatusCode.Should().Be(HttpStatusCode.Created);
        Guid id = (await create.Content.ReadFromJsonAsync<Guid>())!;
        create.Headers.Location!.ToString().Should().Be($"/crm/organizations/{id}");

        OrganizationDto organization = await CrmTestData.GetOrganizationAsync(client, id);
        organization.Name.Should().Be(name);
        organization.TaxIdentifier.Should().Be("TAX-9");
        organization.Address.Should().NotBeNull();
        organization.Address!.Line1.Should().Be("5 Corporate Way");
    }

    [Fact]
    public async Task List_Should_ReturnTheCreatedOrganization()
    {
        HttpClient client = await AdminClientAsync();
        string name = $"Org-{Guid.NewGuid():N}";
        HttpResponseMessage create = await client.PostAsJsonAsync(
            "/crm/organizations",
            new { name, taxIdentifier = (string?)null, address = (object?)null });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        Guid id = (await create.Content.ReadFromJsonAsync<Guid>())!;

        HttpResponseMessage response = await client.GetAsync($"/crm/organizations?search={name}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResult<OrganizationDto>? page = await response.Content.ReadFromJsonAsync<PagedResult<OrganizationDto>>();
        page!.Items.Should().ContainSingle(item => item.Id == id);
    }

    [Fact]
    public async Task Update_Should_ChangeFields_And_ClearAddress()
    {
        HttpClient client = await AdminClientAsync();
        Guid id = await CrmTestData.CreateOrganizationAsync(client);

        HttpResponseMessage update = await client.PutAsJsonAsync(
            $"/crm/organizations/{id}",
            new { name = "Renamed Group", taxIdentifier = "TAX-10", address = (object?)null });

        update.StatusCode.Should().Be(HttpStatusCode.NoContent);

        OrganizationDto organization = await CrmTestData.GetOrganizationAsync(client, id);
        organization.Name.Should().Be("Renamed Group");
        organization.TaxIdentifier.Should().Be("TAX-10");
        organization.Address.Should().BeNull();
    }

    [Fact]
    public async Task Get_Should_ReturnNotFound_WhenOrganizationDoesNotExist()
    {
        HttpClient client = await AdminClientAsync();

        HttpResponseMessage response = await client.GetAsync($"/crm/organizations/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private Task<HttpClient> AdminClientAsync()
        => _factory.CreateAuthenticatedClientAsync(IdentityApiFactory.AdminEmail, IdentityApiFactory.AdminPassword);
}
