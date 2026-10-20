using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Domain.Organizations;
using BSX.Modules.Crm.Infrastructure.IntegrationTests.TestInfrastructure;
using BSX.Modules.Crm.Infrastructure.Repositories;

namespace BSX.Modules.Crm.Infrastructure.IntegrationTests;

public sealed class TenantIsolationTests
{
    [Fact]
    public async Task CustomerRead_Should_NotCrossTenantBoundary()
    {
        await using SqliteCrmDatabase database = await SqliteCrmDatabase.CreateAsync();
        Customer tenantA = TestData.CreateCustomer(tenantId: TestData.TenantA, code: "CUST-A");
        Customer tenantB = TestData.CreateCustomer(tenantId: TestData.TenantB, code: "CUST-B");
        await database.Context.Customers.AddRangeAsync(tenantA, tenantB);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var repository = new CustomerReadRepository(database.Context);

        (await repository.GetByIdAsync(TestData.TenantA, tenantB.Id.Value)).Should().BeNull();
        var page = await repository.ListAsync(TestData.TenantA, 1, 20, null);
        page.Items.Should().ContainSingle();
        page.Items.Single().Code.Should().Be("CUST-A");
    }

    [Fact]
    public async Task OrganizationRead_Should_NotCrossTenantBoundary()
    {
        await using SqliteCrmDatabase database = await SqliteCrmDatabase.CreateAsync();
        Organization tenantA = TestData.CreateOrganization(tenantId: TestData.TenantA, name: "Alpha Group");
        Organization tenantB = TestData.CreateOrganization(tenantId: TestData.TenantB, name: "Beta Group");
        await database.Context.Organizations.AddRangeAsync(tenantA, tenantB);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var repository = new OrganizationReadRepository(database.Context);

        (await repository.GetByIdAsync(TestData.TenantA, tenantB.Id.Value)).Should().BeNull();
        var page = await repository.ListAsync(TestData.TenantA, 1, 20, null);
        page.Items.Should().ContainSingle();
        page.Items.Single().Name.Should().Be("Alpha Group");
    }
}
