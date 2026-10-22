using System.Net;
using System.Net.Http.Json;
using BSX.Modules.Sales.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace BSX.Web.IntegrationTests;

public sealed class SalesOpportunityEndpointsTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public SalesOpportunityEndpointsTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Create_Should_Succeed_AndBeReadable()
    {
        HttpClient client = await AdminClientAsync();
        Guid customerId = await CrmTestData.CreateCustomerAsync(client);
        Guid leadId = await SalesTestData.CreateQualifiedLeadAsync(client, customerId: customerId);
        string title = SalesTestData.NewTitle("Opportunity");

        HttpResponseMessage create = await client.PostAsJsonAsync(
            "/sales/opportunities",
            new
            {
                leadId,
                customerId,
                title,
                amount = 12500.50m,
                currency = "USD",
                probability = 40,
                expectedCloseDate = SalesTestData.FutureCloseDate,
            });

        create.StatusCode.Should().Be(HttpStatusCode.Created);
        Guid id = (await create.Content.ReadFromJsonAsync<Guid>())!;
        create.Headers.Location!.ToString().Should().Be($"/sales/opportunities/{id}");

        OpportunityDto opportunity = await SalesTestData.GetOpportunityAsync(client, id);
        opportunity.LeadId.Should().Be(leadId);
        opportunity.CustomerId.Should().Be(customerId);
        opportunity.Title.Should().Be(title);
        opportunity.Stage.Should().Be("Discovery");
        opportunity.Probability.Should().Be(40);
        opportunity.WeightedAmount.Should().Be(5000.20m);
    }

    [Fact]
    public async Task List_Should_ReturnTheCreatedOpportunity()
    {
        HttpClient client = await AdminClientAsync();
        Guid customerId = await CrmTestData.CreateCustomerAsync(client);
        Guid leadId = await SalesTestData.CreateQualifiedLeadAsync(client, customerId: customerId);
        string title = SalesTestData.NewTitle("Opportunity");
        Guid id = await SalesTestData.CreateOpportunityAsync(client, leadId, customerId, title);

        HttpResponseMessage response = await client.GetAsync($"/sales/opportunities?search={title}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResult<OpportunitySummaryDto>? page =
            await response.Content.ReadFromJsonAsync<PagedResult<OpportunitySummaryDto>>();
        page!.Items.Should().ContainSingle(item => item.Id == id);
    }

    [Fact]
    public async Task Update_Should_ChangeDetailsAndForecast()
    {
        HttpClient client = await AdminClientAsync();
        Guid customerId = await CrmTestData.CreateCustomerAsync(client);
        Guid leadId = await SalesTestData.CreateQualifiedLeadAsync(client, customerId: customerId);
        Guid id = await SalesTestData.CreateOpportunityAsync(client, leadId, customerId);
        string updatedTitle = SalesTestData.NewTitle("Updated");

        HttpResponseMessage update = await client.PutAsJsonAsync(
            $"/sales/opportunities/{id}",
            new
            {
                customerId,
                organizationId = (Guid?)null,
                title = updatedTitle,
                amount = 2000m,
                currency = "EUR",
                probability = 75,
                expectedCloseDate = SalesTestData.FutureCloseDate.AddDays(10),
            });

        update.StatusCode.Should().Be(HttpStatusCode.NoContent);

        OpportunityDto opportunity = await SalesTestData.GetOpportunityAsync(client, id);
        opportunity.Title.Should().Be(updatedTitle);
        opportunity.Amount.Should().Be(2000m);
        opportunity.Currency.Should().Be("EUR");
        opportunity.Probability.Should().Be(75);
        opportunity.WeightedAmount.Should().Be(1500m);
    }

    [Fact]
    public async Task Advance_Should_MoveThroughThePipeline()
    {
        HttpClient client = await AdminClientAsync();
        Guid customerId = await CrmTestData.CreateCustomerAsync(client);
        Guid leadId = await SalesTestData.CreateQualifiedLeadAsync(client, customerId: customerId);
        Guid id = await SalesTestData.CreateOpportunityAsync(client, leadId, customerId);

        foreach (string expectedStage in new[] { "Qualification", "Proposal", "Negotiation" })
        {
            HttpResponseMessage response = await client.PostAsync(
                $"/sales/opportunities/{id}/advance",
                content: null);
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await SalesTestData.GetOpportunityAsync(client, id)).Stage.Should().Be(expectedStage);
        }
    }

    [Fact]
    public async Task Won_Should_TerminateTheOpportunity()
    {
        HttpClient client = await AdminClientAsync();
        Guid customerId = await CrmTestData.CreateCustomerAsync(client);
        Guid leadId = await SalesTestData.CreateQualifiedLeadAsync(client, customerId: customerId);
        Guid id = await SalesTestData.CreateOpportunityAsync(client, leadId, customerId);

        for (int index = 0; index < 3; index++)
        {
            (await client.PostAsync($"/sales/opportunities/{id}/advance", content: null))
                .StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        HttpResponseMessage won = await client.PostAsync($"/sales/opportunities/{id}/won", content: null);
        won.StatusCode.Should().Be(HttpStatusCode.NoContent);

        OpportunityDto opportunity = await SalesTestData.GetOpportunityAsync(client, id);
        opportunity.Stage.Should().Be("Won");
        opportunity.Probability.Should().Be(100);
    }

    [Fact]
    public async Task Won_Should_ReturnConflict_WhenStageIsNotNegotiation()
    {
        HttpClient client = await AdminClientAsync();
        Guid customerId = await CrmTestData.CreateCustomerAsync(client);
        Guid leadId = await SalesTestData.CreateQualifiedLeadAsync(client, customerId: customerId);
        Guid id = await SalesTestData.CreateOpportunityAsync(client, leadId, customerId);

        HttpResponseMessage won = await client.PostAsync($"/sales/opportunities/{id}/won", content: null);

        won.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ProblemDetails? problem = await won.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Title.Should().Be("SALES.Opportunity.InvalidStageTransition");
    }

    [Fact]
    public async Task Lost_Should_TerminateTheOpportunity()
    {
        HttpClient client = await AdminClientAsync();
        Guid customerId = await CrmTestData.CreateCustomerAsync(client);
        Guid leadId = await SalesTestData.CreateQualifiedLeadAsync(client, customerId: customerId);
        Guid id = await SalesTestData.CreateOpportunityAsync(client, leadId, customerId);

        HttpResponseMessage lost = await client.PostAsync($"/sales/opportunities/{id}/lost", content: null);
        lost.StatusCode.Should().Be(HttpStatusCode.NoContent);

        OpportunityDto opportunity = await SalesTestData.GetOpportunityAsync(client, id);
        opportunity.Stage.Should().Be("Lost");
        opportunity.Probability.Should().Be(0);
    }

    [Fact]
    public async Task Create_Should_ReturnConflict_WhenLeadWasAlreadyConverted()
    {
        HttpClient client = await AdminClientAsync();
        Guid customerId = await CrmTestData.CreateCustomerAsync(client);
        Guid leadId = await SalesTestData.CreateQualifiedLeadAsync(client, customerId: customerId);
        await SalesTestData.CreateOpportunityAsync(client, leadId, customerId);

        HttpResponseMessage duplicate = await client.PostAsJsonAsync(
            "/sales/opportunities",
            new
            {
                leadId,
                customerId,
                title = SalesTestData.NewTitle("Duplicate"),
                amount = 100m,
                currency = "USD",
                probability = 10,
                expectedCloseDate = SalesTestData.FutureCloseDate,
            });

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ProblemDetails? problem = await duplicate.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Title.Should().Be("SALES.Opportunity.AlreadyExistsForLead");
    }

    [Fact]
    public async Task Update_Should_ReturnConflict_WhenOpportunityIsTerminal()
    {
        HttpClient client = await AdminClientAsync();
        Guid customerId = await CrmTestData.CreateCustomerAsync(client);
        Guid leadId = await SalesTestData.CreateQualifiedLeadAsync(client, customerId: customerId);
        Guid id = await SalesTestData.CreateOpportunityAsync(client, leadId, customerId);
        (await client.PostAsync($"/sales/opportunities/{id}/lost", content: null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage update = await client.PutAsJsonAsync(
            $"/sales/opportunities/{id}",
            new
            {
                customerId,
                organizationId = (Guid?)null,
                title = SalesTestData.NewTitle("Changed"),
                amount = 100m,
                currency = "USD",
                probability = 10,
                expectedCloseDate = SalesTestData.FutureCloseDate,
            });

        update.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Get_And_Lifecycle_Should_ReturnNotFound_WhenOpportunityDoesNotExist()
    {
        HttpClient client = await AdminClientAsync();
        Guid missing = Guid.NewGuid();

        (await client.GetAsync($"/sales/opportunities/{missing}")).StatusCode
            .Should().Be(HttpStatusCode.NotFound);

        foreach (string action in new[] { "advance", "won", "lost" })
        {
            HttpResponseMessage response = await client.PostAsync(
                $"/sales/opportunities/{missing}/{action}",
                content: null);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }

    private Task<HttpClient> AdminClientAsync()
        => _factory.CreateAuthenticatedClientAsync(IdentityApiFactory.AdminEmail, IdentityApiFactory.AdminPassword);
}
