using System.Net;
using System.Net.Http.Json;

namespace BSX.Web.IntegrationTests.WebFoundation;

public sealed class CrmCustomerWebTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public CrmCustomerWebTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task CustomerJourney_ShouldRenderListDetailCreateAndEditScreens()
    {
        HttpClient api = await _factory.CreateAuthenticatedClientAsync(
            IdentityApiFactory.AdminEmail,
            IdentityApiFactory.AdminPassword);
        string code = CrmTestData.NewCode();

        HttpResponseMessage create = await api.PostAsJsonAsync(
            "/crm/customers",
            new { code, name = "Northwind Traders", taxIdentifier = "TAX-100" });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        Guid id = (await create.Content.ReadFromJsonAsync<Guid>())!;

        HttpClient client = await SignInAsync();

        HttpResponseMessage list = await client.GetAsync("/app/crm/customers");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        string listHtml = await list.Content.ReadAsStringAsync();
        listHtml.Should().Contain("Search and manage the customers in this workspace.");
        listHtml.Should().Contain("New customer");

        HttpResponseMessage search = await client.GetAsync($"/app/crm/customers?search={code}");
        search.StatusCode.Should().Be(HttpStatusCode.OK);
        string searchHtml = await search.Content.ReadAsStringAsync();
        searchHtml.Should().Contain("Northwind Traders");
        searchHtml.Should().Contain("1 customer");

        HttpResponseMessage createPage = await client.GetAsync("/app/crm/customers/new");
        createPage.StatusCode.Should().Be(HttpStatusCode.OK);
        string createHtml = await createPage.Content.ReadAsStringAsync();
        createHtml.Should().Contain("Create a customer record for this workspace.");
        createHtml.Should().Contain("for=\"code\"");
        createHtml.Should().Contain("for=\"name\"");
        createHtml.Should().Contain("for=\"tax-identifier\"");

        HttpResponseMessage detail = await client.GetAsync($"/app/crm/customers/{id}?created=true");
        detail.StatusCode.Should().Be(HttpStatusCode.OK);
        string detailHtml = await detail.Content.ReadAsStringAsync();
        detailHtml.Should().Contain("Northwind Traders");
        detailHtml.Should().Contain(code.ToUpperInvariant());
        detailHtml.Should().Contain("TAX-100");
        detailHtml.Should().Contain("Customer created successfully.");
        detailHtml.Should().Contain("Edit customer");
        detailHtml.Should().Contain("No contacts yet");
        detailHtml.Should().Contain("No addresses yet");

        HttpResponseMessage editPage = await client.GetAsync($"/app/crm/customers/{id}/edit");
        editPage.StatusCode.Should().Be(HttpStatusCode.OK);
        string editHtml = await editPage.Content.ReadAsStringAsync();
        editHtml.Should().Contain("value=\"Northwind Traders\"");
        editHtml.Should().Contain("value=\"TAX-100\"");
        editHtml.Should().Contain("This identifier is permanent.");

        HttpResponseMessage update = await api.PutAsJsonAsync(
            $"/crm/customers/{id}",
            new { name = "Northwind Trading Group", taxIdentifier = "TAX-200" });
        update.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage updatedDetail = await client.GetAsync($"/app/crm/customers/{id}?updated=true");
        updatedDetail.StatusCode.Should().Be(HttpStatusCode.OK);
        string updatedHtml = await updatedDetail.Content.ReadAsStringAsync();
        updatedHtml.Should().Contain("Northwind Trading Group");
        updatedHtml.Should().Contain("TAX-200");
        updatedHtml.Should().Contain("Customer changes saved.");
    }

    [Fact]
    public async Task Detail_ShouldReturnNotFound_WhenTheCustomerDoesNotExist()
    {
        HttpClient client = await SignInAsync();

        HttpResponseMessage response = await client.GetAsync($"/app/crm/customers/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CustomerPages_ShouldRequireAuthentication()
    {
        HttpClient client = WebFoundationTestHelper.CreateClient(_factory);

        HttpResponseMessage list = await client.GetAsync("/app/crm/customers");
        list.StatusCode.Should().Be(HttpStatusCode.Redirect);
        WebFoundationTestHelper.LocationPath(list).Should().StartWith("/login?returnUrl=");

        HttpResponseMessage create = await client.GetAsync("/app/crm/customers/new");
        create.StatusCode.Should().Be(HttpStatusCode.Redirect);
        WebFoundationTestHelper.LocationPath(create).Should().StartWith("/login?returnUrl=");
    }

    [Fact]
    public async Task CustomerPages_ShouldReturnAccessDenied_WhenPermissionIsMissing()
    {
        HttpClient client = await WebFoundationTestHelper.SignInAsync(
            _factory,
            IdentityApiFactory.RestrictedEmail,
            IdentityApiFactory.AdminPassword);

        HttpResponseMessage response = await client.GetAsync("/app/crm/customers/new");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        WebFoundationTestHelper.LocationPath(response).Should().StartWith("/access-denied");
    }

    private Task<HttpClient> SignInAsync()
        => WebFoundationTestHelper.SignInAsync(
            _factory,
            IdentityApiFactory.AdminEmail,
            IdentityApiFactory.AdminPassword);
}
