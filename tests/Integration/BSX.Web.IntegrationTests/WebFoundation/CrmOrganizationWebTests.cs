using System.Net;
using System.Net.Http.Json;
using BSX.Modules.Crm.Application.Dtos;

namespace BSX.Web.IntegrationTests.WebFoundation;

public sealed class CrmOrganizationWebTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public CrmOrganizationWebTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task OrganizationScreens_ShouldRenderListSearchDetailCreateAndEdit()
    {
        HttpClient api = await CreateAdminApiClientAsync();
        (Guid organizationId, string organizationName) = await CreateOrganizationAsync(api);

        HttpClient client = await SignInAsync(IdentityApiFactory.AdminEmail);

        HttpResponseMessage list = await client.GetAsync("/app/crm/organizations");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        string listHtml = await list.Content.ReadAsStringAsync();
        listHtml.Should().Contain("New organization");
        listHtml.Should().Contain("Search organizations");

        HttpResponseMessage search = await client.GetAsync($"/app/crm/organizations?search={organizationName}");
        search.StatusCode.Should().Be(HttpStatusCode.OK);
        string searchHtml = await search.Content.ReadAsStringAsync();
        searchHtml.Should().Contain(organizationName);
        searchHtml.Should().Contain("TAX-ORG-UI");

        HttpResponseMessage detail = await client.GetAsync($"/app/crm/organizations/{organizationId}");
        detail.StatusCode.Should().Be(HttpStatusCode.OK);
        string detailHtml = await detail.Content.ReadAsStringAsync();
        detailHtml.Should().Contain(organizationName);
        detailHtml.Should().Contain("Edit organization");
        detailHtml.Should().Contain("5 Corporate Way");
        detailHtml.Should().Contain("Springfield");

        HttpResponseMessage create = await client.GetAsync("/app/crm/organizations/new");
        create.StatusCode.Should().Be(HttpStatusCode.OK);
        (await create.Content.ReadAsStringAsync()).Should().Contain("Include a primary address");

        HttpResponseMessage edit = await client.GetAsync($"/app/crm/organizations/{organizationId}/edit");
        edit.StatusCode.Should().Be(HttpStatusCode.OK);
        string editHtml = await edit.Content.ReadAsStringAsync();
        editHtml.Should().Contain($"value=\"{organizationName}\"");
        editHtml.Should().Contain("value=\"TAX-ORG-UI\"");
        editHtml.Should().Contain("value=\"5 Corporate Way\"");
    }

    [Fact]
    public async Task CustomerDetail_ShouldExposeAssignmentContactAndAddressManagement()
    {
        HttpClient api = await CreateAdminApiClientAsync();
        (Guid organizationId, string organizationName) = await CreateOrganizationAsync(api);
        Guid customerId = await CrmTestData.CreateCustomerAsync(api, name: "Management Customer");

        HttpResponseMessage assign = await api.PostAsJsonAsync(
            $"/crm/customers/{customerId}/organization",
            new { organizationId });
        assign.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage addContact = await api.PostAsJsonAsync(
            $"/crm/customers/{customerId}/contacts",
            new
            {
                name = "Jane Doe",
                email = "jane@example.com",
                phoneNumber = "+1 555 0100",
                isPrimary = true,
            });
        addContact.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage addAddress = await api.PostAsJsonAsync(
            $"/crm/customers/{customerId}/addresses",
            new
            {
                line1 = "1 Main St",
                line2 = (string?)null,
                city = "Springfield",
                region = (string?)null,
                postalCode = (string?)null,
                country = "US",
                type = "Billing",
            });
        addAddress.StatusCode.Should().Be(HttpStatusCode.NoContent);

        CustomerDto customer = await CrmTestData.GetCustomerAsync(api, customerId);
        Guid contactId = customer.Contacts.Single().Id;
        Guid addressId = customer.Addresses.Single().Id;

        HttpClient client = await SignInAsync(IdentityApiFactory.AdminEmail);
        HttpResponseMessage detail = await client.GetAsync($"/app/crm/customers/{customerId}");
        detail.StatusCode.Should().Be(HttpStatusCode.OK);
        string detailHtml = await detail.Content.ReadAsStringAsync();

        detailHtml.Should().Contain(organizationName);
        detailHtml.Should().Contain("Change organization");
        detailHtml.Should().Contain("Clear organization");
        detailHtml.Should().Contain("Jane Doe");
        detailHtml.Should().Contain("jane@example.com");
        detailHtml.Should().Contain("Primary");
        detailHtml.Should().Contain("Add contact");
        detailHtml.Should().Contain("1 Main St");
        detailHtml.Should().Contain("Default");
        detailHtml.Should().Contain("Add address");

        HttpResponseMessage contactEdit = await client.GetAsync(
            $"/app/crm/customers/{customerId}/contacts/{contactId}/edit");
        contactEdit.StatusCode.Should().Be(HttpStatusCode.OK);
        string contactHtml = await contactEdit.Content.ReadAsStringAsync();
        contactHtml.Should().Contain("value=\"Jane Doe\"");
        contactHtml.Should().Contain("value=\"jane@example.com\"");

        HttpResponseMessage contactCreate = await client.GetAsync(
            $"/app/crm/customers/{customerId}/contacts/new");
        contactCreate.StatusCode.Should().Be(HttpStatusCode.OK);
        (await contactCreate.Content.ReadAsStringAsync())
            .Should().Contain("Make this the primary contact");

        HttpResponseMessage addressEdit = await client.GetAsync(
            $"/app/crm/customers/{customerId}/addresses/{addressId}/edit");
        addressEdit.StatusCode.Should().Be(HttpStatusCode.OK);
        string addressHtml = await addressEdit.Content.ReadAsStringAsync();
        addressHtml.Should().Contain("value=\"1 Main St\"");
        addressHtml.Should().Contain("value=\"Springfield\"");

        HttpResponseMessage addressCreate = await client.GetAsync(
            $"/app/crm/customers/{customerId}/addresses/new");
        addressCreate.StatusCode.Should().Be(HttpStatusCode.OK);
        (await addressCreate.Content.ReadAsStringAsync())
            .Should().Contain("for=\"address-type\"");
    }

    [Fact]
    public async Task ReadOnlyUser_ShouldSeeOrganizationsButNoMutationActions()
    {
        HttpClient api = await CreateAdminApiClientAsync();
        (Guid organizationId, string organizationName) = await CreateOrganizationAsync(api);
        Guid customerId = await CrmTestData.CreateCustomerAsync(api, name: "Read Only Customer");
        await api.PostAsJsonAsync($"/crm/customers/{customerId}/organization", new { organizationId });

        HttpClient client = await SignInAsync(IdentityApiFactory.CrmReaderEmail);

        HttpResponseMessage organizations = await client.GetAsync("/app/crm/organizations");
        organizations.StatusCode.Should().Be(HttpStatusCode.OK);
        string organizationsHtml = await organizations.Content.ReadAsStringAsync();
        organizationsHtml.Should().Contain(organizationName);
        organizationsHtml.Should().NotContain("New organization");

        HttpResponseMessage detail = await client.GetAsync($"/app/crm/organizations/{organizationId}");
        detail.StatusCode.Should().Be(HttpStatusCode.OK);
        (await detail.Content.ReadAsStringAsync()).Should().NotContain("Edit organization");

        HttpResponseMessage customer = await client.GetAsync($"/app/crm/customers/{customerId}");
        customer.StatusCode.Should().Be(HttpStatusCode.OK);
        string customerHtml = await customer.Content.ReadAsStringAsync();
        customerHtml.Should().Contain(organizationName);
        customerHtml.Should().NotContain("Edit customer");
        customerHtml.Should().NotContain("Add contact");
        customerHtml.Should().NotContain("Add address");
        customerHtml.Should().NotContain("Change organization");
        customerHtml.Should().NotContain("Clear organization");
    }

    [Fact]
    public async Task OrganizationPages_ShouldRequireAuthenticationAndPermission()
    {
        HttpClient anonymous = WebFoundationTestHelper.CreateClient(_factory);
        HttpResponseMessage list = await anonymous.GetAsync("/app/crm/organizations");
        list.StatusCode.Should().Be(HttpStatusCode.Redirect);
        WebFoundationTestHelper.LocationPath(list).Should().StartWith("/login?returnUrl=");

        HttpResponseMessage create = await anonymous.GetAsync("/app/crm/organizations/new");
        create.StatusCode.Should().Be(HttpStatusCode.Redirect);

        HttpClient restricted = await WebFoundationTestHelper.SignInAsync(
            _factory,
            IdentityApiFactory.RestrictedEmail,
            IdentityApiFactory.AdminPassword);
        HttpResponseMessage denied = await restricted.GetAsync("/app/crm/organizations");
        denied.StatusCode.Should().Be(HttpStatusCode.Redirect);
        WebFoundationTestHelper.LocationPath(denied).Should().StartWith("/access-denied");
    }

    [Fact]
    public async Task OrganizationDetail_ShouldReturnNotFound_WhenMissing()
    {
        HttpClient client = await SignInAsync(IdentityApiFactory.AdminEmail);

        HttpResponseMessage response = await client.GetAsync($"/app/crm/organizations/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<HttpClient> CreateAdminApiClientAsync()
        => await _factory.CreateAuthenticatedClientAsync(
            IdentityApiFactory.AdminEmail,
            IdentityApiFactory.AdminPassword);

    private Task<HttpClient> SignInAsync(string email)
        => WebFoundationTestHelper.SignInAsync(_factory, email, IdentityApiFactory.AdminPassword);

    private static async Task<(Guid Id, string Name)> CreateOrganizationAsync(HttpClient api)
    {
        string name = $"Org-{Guid.NewGuid():N}";

        HttpResponseMessage response = await api.PostAsJsonAsync(
            "/crm/organizations",
            new
            {
                name,
                taxIdentifier = "TAX-ORG-UI",
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

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Guid id = (await response.Content.ReadFromJsonAsync<Guid>())!;
        return (id, name);
    }
}
