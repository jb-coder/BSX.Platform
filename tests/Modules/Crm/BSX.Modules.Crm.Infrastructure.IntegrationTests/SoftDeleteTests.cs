using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Domain.Organizations;
using BSX.Modules.Crm.Infrastructure.IntegrationTests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Crm.Infrastructure.IntegrationTests;

public sealed class SoftDeleteTests
{
    [Fact]
    public async Task Should_SoftDeleteCustomer_AndExcludeFromQueries()
    {
        await using SqliteCrmDatabase database = await SqliteCrmDatabase.CreateAsync();
        Customer customer = TestData.CreateCustomer();
        await database.Context.Customers.AddAsync(customer);
        await database.Context.SaveChangesAsync();

        database.Context.Customers.Remove(customer);
        await database.Context.SaveChangesAsync();

        (await database.Context.Customers.CountAsync()).Should().Be(0);
        (await database.Context.Customers.IgnoreQueryFilters().CountAsync()).Should().Be(1);
        database.Context.Entry(customer).Property("IsDeleted").CurrentValue.Should().Be(true);
        database.Context.Entry(customer).Property("DeletedOnUtc").CurrentValue.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_SoftDeleteOrganization_AndExcludeFromQueries()
    {
        await using SqliteCrmDatabase database = await SqliteCrmDatabase.CreateAsync();
        Organization organization = TestData.CreateOrganization();
        await database.Context.Organizations.AddAsync(organization);
        await database.Context.SaveChangesAsync();

        database.Context.Organizations.Remove(organization);
        await database.Context.SaveChangesAsync();

        (await database.Context.Organizations.CountAsync()).Should().Be(0);
        (await database.Context.Organizations.IgnoreQueryFilters().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Should_AllowCodeReuse_AfterSoftDelete()
    {
        await using SqliteCrmDatabase database = await SqliteCrmDatabase.CreateAsync();
        Customer first = TestData.CreateCustomer(code: "CUST-001");
        await database.Context.Customers.AddAsync(first);
        await database.Context.SaveChangesAsync();

        database.Context.Customers.Remove(first);
        await database.Context.SaveChangesAsync();

        await database.Context.Customers.AddAsync(TestData.CreateCustomer(code: "CUST-001"));
        Func<Task> act = async () => await database.Context.SaveChangesAsync();

        await act.Should().NotThrowAsync();
    }
}
