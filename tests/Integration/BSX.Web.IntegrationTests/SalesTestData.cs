using System.Net;
using System.Net.Http.Json;
using BSX.Modules.Sales.Application.Dtos;

namespace BSX.Web.IntegrationTests;

/// <summary>Shared Sales endpoint helpers for the Web integration tests.</summary>
internal static class SalesTestData
{
    internal static DateOnly FutureCloseDate => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));

    internal static string NewTitle(string prefix = "Lead") => $"{prefix}-{Guid.NewGuid():N}";

    internal static async Task<Guid> CreateLeadAsync(
        HttpClient client,
        string? title = null,
        Guid? customerId = null,
        Guid? organizationId = null,
        string? notes = null)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/sales/leads",
            new { title = title ?? NewTitle(), customerId, organizationId, notes });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<Guid>())!;
    }

    internal static async Task<LeadDto> GetLeadAsync(HttpClient client, Guid id)
    {
        HttpResponseMessage response = await client.GetAsync($"/sales/leads/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<LeadDto>())!;
    }

    internal static async Task ContactLeadAsync(HttpClient client, Guid id)
    {
        HttpResponseMessage response = await client.PostAsync($"/sales/leads/{id}/contacted", content: null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    internal static async Task QualifyLeadAsync(HttpClient client, Guid id)
    {
        HttpResponseMessage response = await client.PostAsync($"/sales/leads/{id}/qualify", content: null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    internal static async Task<Guid> CreateQualifiedLeadAsync(
        HttpClient client,
        string? title = null,
        Guid? customerId = null,
        Guid? organizationId = null)
    {
        Guid id = await CreateLeadAsync(client, title, customerId, organizationId);
        await ContactLeadAsync(client, id);
        await QualifyLeadAsync(client, id);
        return id;
    }

    internal static async Task<Guid> CreateOpportunityAsync(
        HttpClient client,
        Guid leadId,
        Guid customerId,
        string? title = null,
        decimal amount = 1000m,
        string currency = "USD",
        int probability = 50,
        DateOnly? expectedCloseDate = null)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/sales/opportunities",
            new
            {
                leadId,
                customerId,
                title = title ?? NewTitle("Opportunity"),
                amount,
                currency,
                probability,
                expectedCloseDate = expectedCloseDate ?? FutureCloseDate,
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<Guid>())!;
    }

    internal static async Task<OpportunityDto> GetOpportunityAsync(HttpClient client, Guid id)
    {
        HttpResponseMessage response = await client.GetAsync($"/sales/opportunities/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<OpportunityDto>())!;
    }
}
