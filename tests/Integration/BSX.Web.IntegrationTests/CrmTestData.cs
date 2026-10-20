using System.Net;
using System.Net.Http.Json;
using BSX.Modules.Crm.Application.Dtos;

namespace BSX.Web.IntegrationTests;

/// <summary>Shared CRM endpoint helpers for the Web integration tests.</summary>
internal static class CrmTestData
{
    internal static string NewCode() => $"C-{Guid.NewGuid():N}"[..20];

    internal static async Task<Guid> CreateCustomerAsync(
        HttpClient client,
        string? code = null,
        string name = "Acme Corporation")
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/crm/customers",
            new { code = code ?? NewCode(), name, taxIdentifier = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<Guid>())!;
    }

    internal static async Task<Guid> CreateOrganizationAsync(HttpClient client, string? name = null)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/crm/organizations",
            new
            {
                name = name ?? $"Org-{Guid.NewGuid():N}",
                taxIdentifier = (string?)null,
                address = (object?)null,
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<Guid>())!;
    }

    internal static async Task<CustomerDto> GetCustomerAsync(HttpClient client, Guid id)
    {
        HttpResponseMessage response = await client.GetAsync($"/crm/customers/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<CustomerDto>())!;
    }

    internal static async Task<OrganizationDto> GetOrganizationAsync(HttpClient client, Guid id)
    {
        HttpResponseMessage response = await client.GetAsync($"/crm/organizations/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<OrganizationDto>())!;
    }
}
