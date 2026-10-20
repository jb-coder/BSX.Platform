using System.Net;
using System.Net.Http.Json;
using BSX.Modules.Crm.Application.Dtos;

namespace BSX.Web.IntegrationTests;

public sealed class CrmContactEndpointsTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public CrmContactEndpointsTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Add_Should_CreatePrimaryContact()
    {
        HttpClient client = await AdminClientAsync();
        Guid customerId = await CrmTestData.CreateCustomerAsync(client);

        HttpResponseMessage add = await client.PostAsJsonAsync(
            $"/crm/customers/{customerId}/contacts",
            new
            {
                name = "Jane Doe",
                email = "jane@example.com",
                phoneNumber = "+1 555 0100",
                isPrimary = true,
            });

        add.StatusCode.Should().Be(HttpStatusCode.NoContent);

        CustomerDto customer = await CrmTestData.GetCustomerAsync(client, customerId);
        ContactDto contact = customer.Contacts.Single();
        contact.Name.Should().Be("Jane Doe");
        contact.Email.Should().Be("jane@example.com");
        contact.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task Update_Should_ChangeContactDetails()
    {
        HttpClient client = await AdminClientAsync();
        Guid customerId = await CrmTestData.CreateCustomerAsync(client);
        Guid contactId = await AddContactAsync(client, customerId, "jane@example.com");

        HttpResponseMessage update = await client.PutAsJsonAsync(
            $"/crm/customers/{customerId}/contacts/{contactId}",
            new { name = "Jane Smith", email = "jane.smith@example.com", phoneNumber = (string?)null });

        update.StatusCode.Should().Be(HttpStatusCode.NoContent);

        CustomerDto customer = await CrmTestData.GetCustomerAsync(client, customerId);
        ContactDto contact = customer.Contacts.Single();
        contact.Name.Should().Be("Jane Smith");
        contact.Email.Should().Be("jane.smith@example.com");
    }

    [Fact]
    public async Task SetPrimary_Should_KeepSinglePrimaryContact()
    {
        HttpClient client = await AdminClientAsync();
        Guid customerId = await CrmTestData.CreateCustomerAsync(client);
        Guid firstId = await AddContactAsync(client, customerId, "jane@example.com");
        Guid secondId = await AddContactAsync(client, customerId, "john@example.com");

        HttpResponseMessage setPrimary = await client.PostAsync(
            $"/crm/customers/{customerId}/contacts/{secondId}/primary",
            content: null);

        setPrimary.StatusCode.Should().Be(HttpStatusCode.NoContent);

        CustomerDto customer = await CrmTestData.GetCustomerAsync(client, customerId);
        customer.Contacts.Single(contact => contact.Id == secondId).IsPrimary.Should().BeTrue();
        customer.Contacts.Single(contact => contact.Id == firstId).IsPrimary.Should().BeFalse();
    }

    [Fact]
    public async Task Remove_Should_DeleteContact()
    {
        HttpClient client = await AdminClientAsync();
        Guid customerId = await CrmTestData.CreateCustomerAsync(client);
        Guid contactId = await AddContactAsync(client, customerId, "jane@example.com");

        HttpResponseMessage remove = await client.DeleteAsync($"/crm/customers/{customerId}/contacts/{contactId}");

        remove.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await CrmTestData.GetCustomerAsync(client, customerId)).Contacts.Should().BeEmpty();
    }

    [Fact]
    public async Task Update_Should_ReturnNotFound_WhenContactDoesNotExist()
    {
        HttpClient client = await AdminClientAsync();
        Guid customerId = await CrmTestData.CreateCustomerAsync(client);

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/crm/customers/{customerId}/contacts/{Guid.NewGuid()}",
            new { name = "Missing", email = "missing@example.com", phoneNumber = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<Guid> AddContactAsync(HttpClient client, Guid customerId, string email)
    {
        HttpResponseMessage add = await client.PostAsJsonAsync(
            $"/crm/customers/{customerId}/contacts",
            new { name = "Contact", email, phoneNumber = (string?)null, isPrimary = false });

        add.StatusCode.Should().Be(HttpStatusCode.NoContent);

        CustomerDto customer = await CrmTestData.GetCustomerAsync(client, customerId);
        return customer.Contacts.Single(contact => contact.Email == email).Id;
    }

    private Task<HttpClient> AdminClientAsync()
        => _factory.CreateAuthenticatedClientAsync(IdentityApiFactory.AdminEmail, IdentityApiFactory.AdminPassword);
}
