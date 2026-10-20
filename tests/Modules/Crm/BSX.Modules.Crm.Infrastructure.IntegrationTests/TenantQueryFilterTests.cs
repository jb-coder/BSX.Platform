using BSX.Modules.Crm.Infrastructure.IntegrationTests.TestInfrastructure;
using BSX.Modules.Crm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Crm.Infrastructure.IntegrationTests;

public sealed class TenantQueryFilterTests
{
    [Fact]
    public async Task Should_BeFailClosed_WhenTenantIsMissing()
    {
        await using SqliteCrmDatabase database = await SeedTwoTenantsAsync();

        await using CrmDbContext missingTenant = database.CreateContext(null);

        (await missingTenant.Customers.CountAsync()).Should().Be(0);
        (await missingTenant.Organizations.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Should_ReturnOnlyTheActiveTenant()
    {
        await using SqliteCrmDatabase database = await SeedTwoTenantsAsync();

        await using CrmDbContext tenantA = database.CreateContext(TestData.TenantA);
        await using CrmDbContext tenantB = database.CreateContext(TestData.TenantB);

        (await tenantA.Customers.SingleAsync()).Code.Value.Should().Be("CUST-A");
        (await tenantA.Organizations.SingleAsync()).Name.Value.Should().Be("Alpha Group");

        (await tenantB.Customers.SingleAsync()).Code.Value.Should().Be("CUST-B");
        (await tenantB.Organizations.SingleAsync()).Name.Value.Should().Be("Beta Group");
    }

    [Fact]
    public async Task Should_ReturnNoData_ForTheWrongTenant()
    {
        await using SqliteCrmDatabase database = await SeedTwoTenantsAsync();

        await using CrmDbContext tenantB = database.CreateContext(TestData.TenantB);

        (await tenantB.Customers.CountAsync()).Should().Be(1);
        (await tenantB.Customers.SingleAsync()).Code.Value.Should().Be("CUST-B");
        (await tenantB.Organizations.CountAsync()).Should().Be(1);
        (await tenantB.Organizations.SingleAsync()).Name.Value.Should().Be("Beta Group");
    }

    [Fact]
    public async Task Should_AllowAllTenants_OnlyWithExplicitSystemScope()
    {
        await using SqliteCrmDatabase database = await SeedTwoTenantsAsync();

        await using CrmDbContext system = database.CreateContext(null, isSystem: true);

        (await system.Customers.CountAsync()).Should().Be(2);
        (await system.Organizations.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Should_KeepTenantKeyConsistent_WithTenantId()
    {
        await using SqliteCrmDatabase database = await SeedTwoTenantsAsync();

        await using CrmDbContext tenantA = database.CreateContext(TestData.TenantA);
        var customer = await tenantA.Customers.SingleAsync();

        Guid tenantKey = (Guid)tenantA.Entry(customer).Property("TenantKey").CurrentValue!;
        tenantKey.Should().Be(customer.TenantId.Value);
    }

    private static async Task<SqliteCrmDatabase> SeedTwoTenantsAsync()
    {
        SqliteCrmDatabase database = await SqliteCrmDatabase.CreateAsync();
        await database.Context.Customers.AddRangeAsync(
            TestData.CreateCustomer(tenantId: TestData.TenantA, code: "CUST-A"),
            TestData.CreateCustomer(tenantId: TestData.TenantB, code: "CUST-B"));
        await database.Context.Organizations.AddRangeAsync(
            TestData.CreateOrganization(tenantId: TestData.TenantA, name: "Alpha Group"),
            TestData.CreateOrganization(tenantId: TestData.TenantB, name: "Beta Group"));
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();
        return database;
    }
}
