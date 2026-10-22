using System.Net;
using System.Net.Http.Json;
using BSX.Modules.Sales.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace BSX.Web.IntegrationTests;

/// <summary>
/// End-to-end proof that Sales validates CRM references through the CRM contract, rejects
/// cross-tenant and unknown references, and never surfaces them as HTTP 500 (ADR-011, ADR-015).
/// </summary>
public sealed class SalesCrmValidationEndpointsTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public SalesCrmValidationEndpointsTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Create_Should_AcceptExistingCustomerAndOrganization()
    {
        HttpClient client = await AdminClientAsync();
        Guid customerId = await CrmTestData.CreateCustomerAsync(client);
        Guid organizationId = await CrmTestData.CreateOrganizationAsync(client);

        Guid leadId = await SalesTestData.CreateLeadAsync(
            client,
            customerId: customerId,
            organizationId: organizationId);

        LeadDto lead = await SalesTestData.GetLeadAsync(client, leadId);
        lead.CustomerId.Should().Be(customerId);
        lead.OrganizationId.Should().Be(organizationId);
    }

    [Fact]
    public async Task Create_Should_ReturnConflict_WhenCustomerDoesNotExist()
    {
        HttpClient client = await AdminClientAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/sales/leads",
            new
            {
                title = SalesTestData.NewTitle(),
                customerId = Guid.NewGuid(),
                organizationId = (Guid?)null,
                notes = (string?)null,
            });

        await AssertProblemAsync(response, "SALES.Customer.NotFound");
    }

    [Fact]
    public async Task Create_Should_ReturnConflict_WhenOrganizationDoesNotExist()
    {
        HttpClient client = await AdminClientAsync();
        Guid customerId = await CrmTestData.CreateCustomerAsync(client);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/sales/leads",
            new
            {
                title = SalesTestData.NewTitle(),
                customerId,
                organizationId = Guid.NewGuid(),
                notes = (string?)null,
            });

        await AssertProblemAsync(response, "SALES.Organization.NotFound");
    }

    [Fact]
    public async Task Create_Should_ReturnConflict_WhenCustomerBelongsToAnotherTenant()
    {
        HttpClient tenantA = await AdminClientAsync();
        HttpClient tenantB = await _factory.CreateAuthenticatedClientAsync(
            IdentityApiFactory.TenantAdminEmail,
            IdentityApiFactory.AdminPassword,
            _factory.OtherTenantId);

        Guid customerInTenantB = await CrmTestData.CreateCustomerAsync(tenantB);

        HttpResponseMessage response = await tenantA.PostAsJsonAsync(
            "/sales/leads",
            new
            {
                title = SalesTestData.NewTitle(),
                customerId = customerInTenantB,
                organizationId = (Guid?)null,
                notes = (string?)null,
            });

        await AssertProblemAsync(response, "SALES.Customer.NotFound");
    }

    [Fact]
    public async Task Create_Should_ReturnBadRequest_WhenCustomerIdIsEmpty()
    {
        HttpClient client = await AdminClientAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/sales/leads",
            new
            {
                title = SalesTestData.NewTitle(),
                customerId = Guid.Empty,
                organizationId = (Guid?)null,
                notes = (string?)null,
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Title.Should().Be("Validation.Failed");
    }

    [Fact]
    public async Task CreateOpportunity_Should_ReturnConflict_WhenCustomerDoesNotExist()
    {
        HttpClient client = await AdminClientAsync();
        Guid leadCustomer = await CrmTestData.CreateCustomerAsync(client);
        Guid leadId = await SalesTestData.CreateQualifiedLeadAsync(client, customerId: leadCustomer);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/sales/opportunities",
            new
            {
                leadId,
                customerId = Guid.NewGuid(),
                title = SalesTestData.NewTitle("Opportunity"),
                amount = 1000m,
                currency = "USD",
                probability = 50,
                expectedCloseDate = SalesTestData.FutureCloseDate,
            });

        await AssertProblemAsync(response, "SALES.Customer.NotFound");
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, string expectedTitle)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Title.Should().Be(expectedTitle);
    }

    private Task<HttpClient> AdminClientAsync()
        => _factory.CreateAuthenticatedClientAsync(IdentityApiFactory.AdminEmail, IdentityApiFactory.AdminPassword);
}
