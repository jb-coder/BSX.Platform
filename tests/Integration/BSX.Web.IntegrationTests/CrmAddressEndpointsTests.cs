using System.Net;
using System.Net.Http.Json;
using BSX.Modules.Crm.Application.Dtos;

namespace BSX.Web.IntegrationTests;

public sealed class CrmAddressEndpointsTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public CrmAddressEndpointsTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Add_Should_CreateAddress()
    {
        HttpClient client = await AdminClientAsync();
        Guid customerId = await CrmTestData.CreateCustomerAsync(client);

        HttpResponseMessage add = await client.PostAsJsonAsync(
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

        add.StatusCode.Should().Be(HttpStatusCode.NoContent);

        CustomerDto customer = await CrmTestData.GetCustomerAsync(client, customerId);
        AddressDto address = customer.Addresses.Single();
        address.Line1.Should().Be("1 Main St");
        address.Type.Should().Be("Billing");
        address.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task Update_Should_ChangeAddressAndType()
    {
        HttpClient client = await AdminClientAsync();
        Guid customerId = await CrmTestData.CreateCustomerAsync(client);
        Guid addressId = await AddAddressAsync(client, customerId, "1 Main St", "Billing");

        HttpResponseMessage update = await client.PutAsJsonAsync(
            $"/crm/customers/{customerId}/addresses/{addressId}",
            new
            {
                line1 = "9 New St",
                line2 = (string?)null,
                city = "Springfield",
                region = (string?)null,
                postalCode = (string?)null,
                country = "US",
                type = "Shipping",
            });

        update.StatusCode.Should().Be(HttpStatusCode.NoContent);

        CustomerDto customer = await CrmTestData.GetCustomerAsync(client, customerId);
        AddressDto address = customer.Addresses.Single();
        address.Line1.Should().Be("9 New St");
        address.Type.Should().Be("Shipping");
    }

    [Fact]
    public async Task SetDefault_Should_KeepSingleDefaultAddress()
    {
        HttpClient client = await AdminClientAsync();
        Guid customerId = await CrmTestData.CreateCustomerAsync(client);
        Guid firstId = await AddAddressAsync(client, customerId, "1 Main St", "Billing");
        Guid secondId = await AddAddressAsync(client, customerId, "2 Oak St", "Shipping");

        HttpResponseMessage setDefault = await client.PostAsync(
            $"/crm/customers/{customerId}/addresses/{secondId}/default",
            content: null);

        setDefault.StatusCode.Should().Be(HttpStatusCode.NoContent);

        CustomerDto customer = await CrmTestData.GetCustomerAsync(client, customerId);
        customer.Addresses.Single(address => address.Id == secondId).IsDefault.Should().BeTrue();
        customer.Addresses.Single(address => address.Id == firstId).IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task Remove_Should_DeleteAddress()
    {
        HttpClient client = await AdminClientAsync();
        Guid customerId = await CrmTestData.CreateCustomerAsync(client);
        Guid addressId = await AddAddressAsync(client, customerId, "1 Main St", "Billing");

        HttpResponseMessage remove = await client.DeleteAsync($"/crm/customers/{customerId}/addresses/{addressId}");

        remove.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await CrmTestData.GetCustomerAsync(client, customerId)).Addresses.Should().BeEmpty();
    }

    [Fact]
    public async Task Add_Should_RejectUnknownAddressType()
    {
        HttpClient client = await AdminClientAsync();
        Guid customerId = await CrmTestData.CreateCustomerAsync(client);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/crm/customers/{customerId}/addresses",
            new
            {
                line1 = "1 Main St",
                line2 = (string?)null,
                city = "Springfield",
                region = (string?)null,
                postalCode = (string?)null,
                country = "US",
                type = "Bogus",
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    private async Task<Guid> AddAddressAsync(HttpClient client, Guid customerId, string line1, string type)
    {
        HttpResponseMessage add = await client.PostAsJsonAsync(
            $"/crm/customers/{customerId}/addresses",
            new
            {
                line1,
                line2 = (string?)null,
                city = "Springfield",
                region = (string?)null,
                postalCode = (string?)null,
                country = "US",
                type,
            });

        add.StatusCode.Should().Be(HttpStatusCode.NoContent);

        CustomerDto customer = await CrmTestData.GetCustomerAsync(client, customerId);
        return customer.Addresses.Single(address => address.Line1 == line1).Id;
    }

    private Task<HttpClient> AdminClientAsync()
        => _factory.CreateAuthenticatedClientAsync(IdentityApiFactory.AdminEmail, IdentityApiFactory.AdminPassword);
}
