using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace BSX.Web.IntegrationTests;

/// <summary>Optimistic-concurrency failures must map to the platform 409 problem contract.</summary>
public sealed class SalesConcurrencyEndpointsTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public SalesConcurrencyEndpointsTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Update_Should_ReturnConflict_WhenConcurrentModificationIsDetected()
    {
        HttpClient client = await _factory.CreateAuthenticatedClientAsync(
            IdentityApiFactory.AdminEmail,
            IdentityApiFactory.AdminPassword);
        string title = SalesTestData.NewTitle();
        Guid id = await SalesTestData.CreateLeadAsync(client, title);

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/sales/leads/{id}",
            new
            {
                title = ForcedSalesConcurrencySaveChangesInterceptor.TriggerTitle,
                customerId = (Guid?)null,
                organizationId = (Guid?)null,
                notes = (string?)null,
            });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Title.Should().Be("SALES.ConcurrencyConflict");

        (await SalesTestData.GetLeadAsync(client, id)).Title.Should().Be(title);
    }
}
