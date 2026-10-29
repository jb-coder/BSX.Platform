using System.Net;

namespace BSX.Web.IntegrationTests.WebFoundation;

public sealed class SalesWebTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public SalesWebTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task LeadScreens_ShouldRenderListDetailCreateAndEdit()
    {
        HttpClient api = await CreateAdminApiClientAsync();
        Guid customerId = await CrmTestData.CreateCustomerAsync(api, name: "Sales UI Customer");
        Guid organizationId = await CrmTestData.CreateOrganizationAsync(api, "Sales UI Organization");
        string leadTitle = SalesTestData.NewTitle("Sales UI Lead");
        Guid leadId = await SalesTestData.CreateLeadAsync(
            api,
            leadTitle,
            customerId,
            organizationId,
            "Initial sales note");

        HttpClient client = await SignInAsync(IdentityApiFactory.AdminEmail);

        HttpResponseMessage list = await client.GetAsync("/app/sales/leads");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        string listHtml = await list.Content.ReadAsStringAsync();
        listHtml.Should().Contain("New lead");
        listHtml.Should().Contain("Search leads");

        HttpResponseMessage search = await client.GetAsync($"/app/sales/leads?search={Uri.EscapeDataString(leadTitle)}");
        search.StatusCode.Should().Be(HttpStatusCode.OK);
        string searchHtml = await search.Content.ReadAsStringAsync();
        searchHtml.Should().Contain(leadTitle);
        searchHtml.Should().Contain("New");

        HttpResponseMessage create = await client.GetAsync("/app/sales/leads/new");
        create.StatusCode.Should().Be(HttpStatusCode.OK);
        string createHtml = await create.Content.ReadAsStringAsync();
        createHtml.Should().Contain("for=\"lead-title\"");
        createHtml.Should().Contain("for=\"lead-notes\"");

        HttpResponseMessage linkedCreate = await client.GetAsync(
            $"/app/sales/leads/new?customerId={customerId}&organizationId={organizationId}");
        linkedCreate.StatusCode.Should().Be(HttpStatusCode.OK);
        string linkedHtml = await linkedCreate.Content.ReadAsStringAsync();
        linkedHtml.Should().Contain("Sales UI Customer");
        linkedHtml.Should().Contain("Sales UI Organization");

        HttpResponseMessage detail = await client.GetAsync($"/app/sales/leads/{leadId}");
        detail.StatusCode.Should().Be(HttpStatusCode.OK);
        string detailHtml = await detail.Content.ReadAsStringAsync();
        detailHtml.Should().Contain(leadTitle);
        detailHtml.Should().Contain("Sales UI Customer");
        detailHtml.Should().Contain("Sales UI Organization");
        detailHtml.Should().Contain("Mark contacted");
        detailHtml.Should().Contain("Disqualify");
        detailHtml.Should().Contain("Initial sales note");

        HttpResponseMessage edit = await client.GetAsync($"/app/sales/leads/{leadId}/edit");
        edit.StatusCode.Should().Be(HttpStatusCode.OK);
        string editHtml = await edit.Content.ReadAsStringAsync();
        editHtml.Should().Contain($"value=\"{leadTitle}\"");
        editHtml.Should().Contain("Initial sales note");
        editHtml.Should().Contain("Sales UI Customer");
    }

    [Fact]
    public async Task QualifiedLeadAndOpportunityScreens_ShouldRenderLifecycle()
    {
        HttpClient api = await CreateAdminApiClientAsync();
        Guid customerId = await CrmTestData.CreateCustomerAsync(api, name: "Opportunity Customer");
        Guid leadId = await SalesTestData.CreateQualifiedLeadAsync(
            api,
            SalesTestData.NewTitle("Qualified UI Lead"),
            customerId);
        Guid opportunityId = await SalesTestData.CreateOpportunityAsync(
            api,
            leadId,
            customerId,
            "Enterprise UI Opportunity",
            12500.50m,
            "USD",
            40);

        HttpClient client = await SignInAsync(IdentityApiFactory.AdminEmail);

        HttpResponseMessage leadDetail = await client.GetAsync($"/app/sales/leads/{leadId}");
        leadDetail.StatusCode.Should().Be(HttpStatusCode.OK);
        (await leadDetail.Content.ReadAsStringAsync()).Should().Contain("Create opportunity");

        HttpResponseMessage startOpportunity = await client.GetAsync("/app/sales/opportunities/new");
        startOpportunity.StatusCode.Should().Be(HttpStatusCode.OK);
        (await startOpportunity.Content.ReadAsStringAsync()).Should().Contain("Start with a qualified lead");

        HttpResponseMessage createOpportunity = await client.GetAsync($"/app/sales/opportunities/new?leadId={leadId}");
        createOpportunity.StatusCode.Should().Be(HttpStatusCode.OK);
        string createHtml = await createOpportunity.Content.ReadAsStringAsync();
        createHtml.Should().Contain("for=\"opportunity-title\"");
        createHtml.Should().Contain("Opportunity Customer");

        HttpResponseMessage list = await client.GetAsync("/app/sales/opportunities");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        string listHtml = await list.Content.ReadAsStringAsync();
        listHtml.Should().Contain("New opportunity");
        listHtml.Should().Contain("Search opportunities");

        HttpResponseMessage detail = await client.GetAsync($"/app/sales/opportunities/{opportunityId}");
        detail.StatusCode.Should().Be(HttpStatusCode.OK);
        string detailHtml = await detail.Content.ReadAsStringAsync();
        detailHtml.Should().Contain("Enterprise UI Opportunity");
        detailHtml.Should().Contain("12,500.50 USD");
        detailHtml.Should().Contain("40%");
        detailHtml.Should().Contain("Discovery");
        detailHtml.Should().Contain("Advance to Qualification");
        detailHtml.Should().Contain("Mark lost");
        detailHtml.Should().Contain("aria-label=\"Opportunity stage timeline\"");

        HttpResponseMessage edit = await client.GetAsync($"/app/sales/opportunities/{opportunityId}/edit");
        edit.StatusCode.Should().Be(HttpStatusCode.OK);
        string editHtml = await edit.Content.ReadAsStringAsync();
        editHtml.Should().Contain("value=\"Enterprise UI Opportunity\"");
        editHtml.Should().Contain("value=\"USD\"");
        editHtml.Should().Contain("Opportunity Customer");
    }

    [Fact]
    public async Task SalesReader_ShouldSeeReadOnlyScreensWithoutMutationActions()
    {
        HttpClient api = await CreateAdminApiClientAsync();
        Guid customerId = await CrmTestData.CreateCustomerAsync(api, name: "Read Only Sales Customer");
        Guid leadId = await SalesTestData.CreateQualifiedLeadAsync(
            api,
            SalesTestData.NewTitle("Read Only Lead"),
            customerId);
        Guid opportunityId = await SalesTestData.CreateOpportunityAsync(
            api,
            leadId,
            customerId,
            "Read Only Opportunity");

        HttpClient client = await SignInAsync(IdentityApiFactory.SalesReaderEmail);

        HttpResponseMessage leads = await client.GetAsync("/app/sales/leads");
        leads.StatusCode.Should().Be(HttpStatusCode.OK);
        string leadsHtml = await leads.Content.ReadAsStringAsync();
        leadsHtml.Should().Contain("Read Only Lead");
        leadsHtml.Should().NotContain("New lead");

        HttpResponseMessage leadDetail = await client.GetAsync($"/app/sales/leads/{leadId}");
        leadDetail.StatusCode.Should().Be(HttpStatusCode.OK);
        string leadHtml = await leadDetail.Content.ReadAsStringAsync();
        leadHtml.Should().NotContain("Edit lead");
        leadHtml.Should().NotContain("Create opportunity");
        leadHtml.Should().NotContain("Disqualify");

        HttpResponseMessage opportunities = await client.GetAsync("/app/sales/opportunities");
        opportunities.StatusCode.Should().Be(HttpStatusCode.OK);
        string opportunitiesHtml = await opportunities.Content.ReadAsStringAsync();
        opportunitiesHtml.Should().Contain("Read Only Opportunity");
        opportunitiesHtml.Should().NotContain("New opportunity");

        HttpResponseMessage opportunityDetail = await client.GetAsync($"/app/sales/opportunities/{opportunityId}");
        opportunityDetail.StatusCode.Should().Be(HttpStatusCode.OK);
        string opportunityHtml = await opportunityDetail.Content.ReadAsStringAsync();
        opportunityHtml.Should().NotContain("Edit opportunity");
        opportunityHtml.Should().NotContain("Advance to");
        opportunityHtml.Should().NotContain("Mark lost");
    }

    [Fact]
    public async Task SalesPages_ShouldRequireAuthenticationAndPermission()
    {
        HttpClient anonymous = WebFoundationTestHelper.CreateClient(_factory);

        HttpResponseMessage leads = await anonymous.GetAsync("/app/sales/leads");
        leads.StatusCode.Should().Be(HttpStatusCode.Redirect);
        WebFoundationTestHelper.LocationPath(leads).Should().StartWith("/login?returnUrl=");

        HttpResponseMessage opportunities = await anonymous.GetAsync("/app/sales/opportunities");
        opportunities.StatusCode.Should().Be(HttpStatusCode.Redirect);

        HttpClient restricted = await WebFoundationTestHelper.SignInAsync(
            _factory,
            IdentityApiFactory.RestrictedEmail,
            IdentityApiFactory.AdminPassword);

        HttpResponseMessage denied = await restricted.GetAsync("/app/sales/leads");
        denied.StatusCode.Should().Be(HttpStatusCode.Redirect);
        WebFoundationTestHelper.LocationPath(denied).Should().StartWith("/access-denied");
    }

    [Fact]
    public async Task SalesDetails_ShouldReturnNotFound_WhenMissing()
    {
        HttpClient client = await SignInAsync(IdentityApiFactory.AdminEmail);

        HttpResponseMessage lead = await client.GetAsync($"/app/sales/leads/{Guid.NewGuid()}");
        lead.StatusCode.Should().Be(HttpStatusCode.NotFound);

        HttpResponseMessage opportunity = await client.GetAsync($"/app/sales/opportunities/{Guid.NewGuid()}");
        opportunity.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private Task<HttpClient> CreateAdminApiClientAsync()
        => _factory.CreateAuthenticatedClientAsync(
            IdentityApiFactory.AdminEmail,
            IdentityApiFactory.AdminPassword);

    private Task<HttpClient> SignInAsync(string email)
        => WebFoundationTestHelper.SignInAsync(_factory, email, IdentityApiFactory.AdminPassword);
}
