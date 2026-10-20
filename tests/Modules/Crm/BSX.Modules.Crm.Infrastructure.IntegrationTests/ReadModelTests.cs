using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Domain.Organizations;
using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.Modules.Crm.Infrastructure.IntegrationTests.TestInfrastructure;
using BSX.Modules.Crm.Infrastructure.Repositories;

namespace BSX.Modules.Crm.Infrastructure.IntegrationTests;

public sealed class ReadModelTests
{
    [Fact]
    public async Task Should_ProjectCustomerDetail_WithAddressesAndContacts()
    {
        await using SqliteCrmDatabase database = await SqliteCrmDatabase.CreateAsync();
        Guid organizationId = Guid.NewGuid();
        Customer customer = TestData.CreateCustomer();
        customer.AssignOrganization(OrganizationId.From(organizationId)).IsSuccess.Should().BeTrue();
        TestData.AddAddress(customer);
        TestData.AddContact(customer);
        await database.Context.Customers.AddAsync(customer);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var repository = new CustomerReadRepository(database.Context);
        var dto = await repository.GetByIdAsync(TestData.TenantA, customer.Id.Value);

        dto.Should().NotBeNull();
        dto!.Code.Should().Be("CUST-001");
        dto.Name.Should().Be("Acme Corporation");
        dto.Status.Should().Be("Active");
        dto.OrganizationId.Should().Be(organizationId);
        dto.Addresses.Should().ContainSingle();
        dto.Addresses.Single().Line1.Should().Be("1 Main St");
        dto.Addresses.Single().Type.Should().Be("Billing");
        dto.Addresses.Single().IsDefault.Should().BeTrue();
        dto.Contacts.Should().ContainSingle();
        dto.Contacts.Single().Name.Should().Be("Jane Doe");
        dto.Contacts.Single().Email.Should().Be("jane@example.com");
        dto.Contacts.Single().IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task Should_ProjectCustomerSummary_AndPage()
    {
        await using SqliteCrmDatabase database = await SqliteCrmDatabase.CreateAsync();
        for (int index = 1; index <= 3; index++)
        {
            await database.Context.Customers.AddAsync(TestData.CreateCustomer(code: $"CUST-{index:000}"));
        }

        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var repository = new CustomerReadRepository(database.Context);
        var page = await repository.ListAsync(TestData.TenantA, 1, 2, null);

        page.TotalCount.Should().Be(3);
        page.Items.Should().HaveCount(2);
        page.Page.Should().Be(1);
        page.Size.Should().Be(2);
    }

    [Fact]
    public async Task Should_ProjectOrganizationDetail_WithAddress()
    {
        await using SqliteCrmDatabase database = await SqliteCrmDatabase.CreateAsync();
        Organization organization = TestData.CreateOrganization(address: TestData.Address());
        await database.Context.Organizations.AddAsync(organization);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var repository = new OrganizationReadRepository(database.Context);
        var dto = await repository.GetByIdAsync(TestData.TenantA, organization.Id.Value);

        dto.Should().NotBeNull();
        dto!.Name.Should().Be("Holding Group");
        dto.TaxIdentifier.Should().Be("TAX-999");
        dto.Address.Should().NotBeNull();
        dto.Address!.Line1.Should().Be("1 Main St");
        dto.Address.Type.Should().Be("Other");
    }
}
