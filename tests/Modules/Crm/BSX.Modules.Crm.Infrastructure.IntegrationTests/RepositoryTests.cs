using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Domain.Organizations;
using BSX.Modules.Crm.Infrastructure.IntegrationTests.TestInfrastructure;
using BSX.Modules.Crm.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Crm.Infrastructure.IntegrationTests;

public sealed class RepositoryTests
{
    [Fact]
    public async Task Should_RoundTripCustomer_WithOwnedCollections()
    {
        await using SqliteCrmDatabase database = await SqliteCrmDatabase.CreateAsync();
        Customer customer = TestData.CreateCustomer();
        TestData.AddAddress(customer);
        TestData.AddContact(customer);
        await database.Context.Customers.AddAsync(customer);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var repository = new CustomerRepository(database.Context);
        Customer? loaded = await repository.GetByIdAsync(TestData.Tenant(), customer.Id);

        loaded.Should().NotBeNull();
        loaded!.Addresses.Should().ContainSingle();
        loaded.Contacts.Should().ContainSingle();
        loaded.Addresses.Single().Address.Line1.Should().Be("1 Main St");
        loaded.Contacts.Single().Email!.Value.Should().Be("jane@example.com");
    }

    [Fact]
    public async Task Should_RoundTripOrganization_WithAddress()
    {
        await using SqliteCrmDatabase database = await SqliteCrmDatabase.CreateAsync();
        Organization organization = TestData.CreateOrganization(address: TestData.Address());
        await database.Context.Organizations.AddAsync(organization);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var repository = new OrganizationRepository(database.Context);
        Organization? loaded = await repository.GetByIdAsync(TestData.Tenant(), organization.Id);

        loaded.Should().NotBeNull();
        loaded!.Address.Should().NotBeNull();
        loaded.Address!.Line1.Should().Be("1 Main St");
    }

    [Fact]
    public async Task Should_NotReturnCustomer_FromAnotherTenant()
    {
        await using SqliteCrmDatabase database = await SqliteCrmDatabase.CreateAsync();
        Customer customer = TestData.CreateCustomer(tenantId: TestData.TenantA);
        await database.Context.Customers.AddAsync(customer);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var repository = new CustomerRepository(database.Context);
        Customer? crossTenant = await repository.GetByIdAsync(TestData.Tenant(TestData.TenantB), customer.Id);

        crossTenant.Should().BeNull();
    }

    [Fact]
    public async Task Should_DetectDuplicateCustomerCode_WithinTenant()
    {
        await using SqliteCrmDatabase database = await SqliteCrmDatabase.CreateAsync();
        await database.Context.Customers.AddRangeAsync(
            TestData.CreateCustomer(code: "CUST-001"),
            TestData.CreateCustomer(code: "CUST-001"));

        Func<Task> act = async () => await database.Context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Should_AllowSameCustomerCode_InDifferentTenants()
    {
        await using SqliteCrmDatabase database = await SqliteCrmDatabase.CreateAsync();
        await database.Context.Customers.AddRangeAsync(
            TestData.CreateCustomer(tenantId: TestData.TenantA, code: "CUST-001"),
            TestData.CreateCustomer(tenantId: TestData.TenantB, code: "CUST-001"));

        await database.Context.SaveChangesAsync();

        int count = await database.Context.Customers.IgnoreQueryFilters().CountAsync();
        count.Should().Be(2);
    }

    [Fact]
    public async Task Should_ExistsByCode_RespectTenant()
    {
        await using SqliteCrmDatabase database = await SqliteCrmDatabase.CreateAsync();
        await database.Context.Customers.AddAsync(TestData.CreateCustomer(tenantId: TestData.TenantA, code: "CUST-001"));
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var repository = new CustomerRepository(database.Context);

        (await repository.ExistsByCodeAsync(TestData.Tenant(TestData.TenantA), TestData.Code("CUST-001")))
            .Should().BeTrue();
        (await repository.ExistsByCodeAsync(TestData.Tenant(TestData.TenantB), TestData.Code("CUST-001")))
            .Should().BeFalse();
    }
}
