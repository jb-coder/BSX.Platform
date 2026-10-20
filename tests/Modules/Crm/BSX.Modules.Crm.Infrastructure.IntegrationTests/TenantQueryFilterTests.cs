using BSX.Modules.Crm.Infrastructure.IntegrationTests.TestInfrastructure;
using BSX.Modules.Crm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Crm.Infrastructure.IntegrationTests;

public sealed class TenantQueryFilterTests
{
    [Fact]
    public async Task Should_FilterCustomersAndOrganizations_ByAmbientTenant()
    {
        await using SqliteCrmDatabase database = await SqliteCrmDatabase.CreateAsync();
        await database.Context.Customers.AddRangeAsync(
            TestData.CreateCustomer(tenantId: TestData.TenantA, code: "CUST-A"),
            TestData.CreateCustomer(tenantId: TestData.TenantB, code: "CUST-B"));
        await database.Context.Organizations.AddRangeAsync(
            TestData.CreateOrganization(tenantId: TestData.TenantA, name: "Alpha Group"),
            TestData.CreateOrganization(tenantId: TestData.TenantB, name: "Beta Group"));
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        await using CrmDbContext tenantA = database.CreateContext(TestData.TenantA);

        (await tenantA.Customers.CountAsync()).Should().Be(1);
        (await tenantA.Customers.SingleAsync()).Code.Value.Should().Be("CUST-A");
        (await tenantA.Organizations.CountAsync()).Should().Be(1);
        (await tenantA.Organizations.SingleAsync()).Name.Value.Should().Be("Alpha Group");
        (await tenantA.Customers.IgnoreQueryFilters().CountAsync()).Should().Be(2);
    }
}
