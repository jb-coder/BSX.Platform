using System.Net;
using System.Net.Http.Json;
using BSX.Modules.Sales.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace BSX.Web.IntegrationTests;

public sealed class SalesLeadEndpointsTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public SalesLeadEndpointsTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Create_Should_Succeed_AndBeReadable()
    {
        HttpClient client = await AdminClientAsync();
        string title = SalesTestData.NewTitle();

        HttpResponseMessage create = await client.PostAsJsonAsync(
            "/sales/leads",
            new { title, customerId = (Guid?)null, organizationId = (Guid?)null, notes = "Initial note" });

        create.StatusCode.Should().Be(HttpStatusCode.Created);
        Guid id = (await create.Content.ReadFromJsonAsync<Guid>())!;
        create.Headers.Location!.ToString().Should().Be($"/sales/leads/{id}");

        LeadDto lead = await SalesTestData.GetLeadAsync(client, id);
        lead.Id.Should().Be(id);
        lead.TenantId.Should().Be(_factory.TenantId);
        lead.Title.Should().Be(title);
        lead.Status.Should().Be("New");
        lead.Notes.Should().Be("Initial note");
    }

    [Fact]
    public async Task List_Should_ReturnTheCreatedLead()
    {
        HttpClient client = await AdminClientAsync();
        string title = SalesTestData.NewTitle();
        Guid id = await SalesTestData.CreateLeadAsync(client, title);

        HttpResponseMessage response = await client.GetAsync($"/sales/leads?search={title}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResult<LeadSummaryDto>? page =
            await response.Content.ReadFromJsonAsync<PagedResult<LeadSummaryDto>>();
        page!.Items.Should().ContainSingle(item => item.Id == id);
    }

    [Fact]
    public async Task Update_Should_ChangeTitleAndNotes()
    {
        HttpClient client = await AdminClientAsync();
        Guid id = await SalesTestData.CreateLeadAsync(client);
        string updatedTitle = SalesTestData.NewTitle("Updated");

        HttpResponseMessage update = await client.PutAsJsonAsync(
            $"/sales/leads/{id}",
            new { title = updatedTitle, customerId = (Guid?)null, organizationId = (Guid?)null, notes = "Updated" });

        update.StatusCode.Should().Be(HttpStatusCode.NoContent);

        LeadDto lead = await SalesTestData.GetLeadAsync(client, id);
        lead.Title.Should().Be(updatedTitle);
        lead.Notes.Should().Be("Updated");
    }

    [Fact]
    public async Task Contacted_Should_MoveLeadToContacted()
    {
        HttpClient client = await AdminClientAsync();
        Guid id = await SalesTestData.CreateLeadAsync(client);

        await SalesTestData.ContactLeadAsync(client, id);

        (await SalesTestData.GetLeadAsync(client, id)).Status.Should().Be("Contacted");
    }

    [Fact]
    public async Task Qualify_Should_MoveContactedLeadToQualified()
    {
        HttpClient client = await AdminClientAsync();
        Guid id = await SalesTestData.CreateLeadAsync(client);

        await SalesTestData.ContactLeadAsync(client, id);
        await SalesTestData.QualifyLeadAsync(client, id);

        (await SalesTestData.GetLeadAsync(client, id)).Status.Should().Be("Qualified");
    }

    [Fact]
    public async Task Qualify_Should_ReturnConflict_WhenLeadWasNotContacted()
    {
        HttpClient client = await AdminClientAsync();
        Guid id = await SalesTestData.CreateLeadAsync(client);

        HttpResponseMessage response = await client.PostAsync($"/sales/leads/{id}/qualify", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Title.Should().Be("SALES.Lead.MustBeContacted");
    }

    [Fact]
    public async Task Disqualify_Should_MoveLeadToDisqualified()
    {
        HttpClient client = await AdminClientAsync();
        Guid id = await SalesTestData.CreateLeadAsync(client);

        HttpResponseMessage response = await client.PostAsync($"/sales/leads/{id}/disqualify", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await SalesTestData.GetLeadAsync(client, id)).Status.Should().Be("Disqualified");
    }

    [Fact]
    public async Task Update_Should_AllowNotesOnly_OnQualifiedLead()
    {
        HttpClient client = await AdminClientAsync();
        string title = SalesTestData.NewTitle();
        Guid id = await SalesTestData.CreateQualifiedLeadAsync(client, title);

        HttpResponseMessage notesOnly = await client.PutAsJsonAsync(
            $"/sales/leads/{id}",
            new { title, customerId = (Guid?)null, organizationId = (Guid?)null, notes = "After qualification" });

        notesOnly.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage changeDetails = await client.PutAsJsonAsync(
            $"/sales/leads/{id}",
            new
            {
                title = SalesTestData.NewTitle("Changed"),
                customerId = (Guid?)null,
                organizationId = (Guid?)null,
                notes = "Changed",
            });

        changeDetails.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Get_And_Update_Should_ReturnNotFound_WhenLeadDoesNotExist()
    {
        HttpClient client = await AdminClientAsync();
        Guid missing = Guid.NewGuid();

        HttpResponseMessage get = await client.GetAsync($"/sales/leads/{missing}");
        get.StatusCode.Should().Be(HttpStatusCode.NotFound);

        HttpResponseMessage update = await client.PutAsJsonAsync(
            $"/sales/leads/{missing}",
            new
            {
                title = SalesTestData.NewTitle(),
                customerId = (Guid?)null,
                organizationId = (Guid?)null,
                notes = (string?)null,
            });

        update.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Lifecycle_Should_ReturnNotFound_WhenLeadDoesNotExist()
    {
        HttpClient client = await AdminClientAsync();
        Guid missing = Guid.NewGuid();

        foreach (string action in new[] { "contacted", "qualify", "disqualify" })
        {
            HttpResponseMessage response = await client.PostAsync($"/sales/leads/{missing}/{action}", content: null);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }

    private Task<HttpClient> AdminClientAsync()
        => _factory.CreateAuthenticatedClientAsync(IdentityApiFactory.AdminEmail, IdentityApiFactory.AdminPassword);
}
