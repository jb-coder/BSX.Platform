using BSX.Modules.Crm.Domain.Organizations;
using BSX.Modules.Crm.Infrastructure.IntegrationTests.TestInfrastructure;
using BSX.Modules.Crm.Infrastructure.Repositories;

namespace BSX.Modules.Crm.Infrastructure.IntegrationTests;

public sealed class SearchTests
{
    [Fact]
    public async Task CustomerSearch_Should_MatchCodeOrName_CaseInsensitive()
    {
        await using SqliteCrmDatabase database = await SqliteCrmDatabase.CreateAsync();
        await database.Context.Customers.AddRangeAsync(
            TestData.CreateCustomer(code: "CUST-001", name: "Acme Corporation"),
            TestData.CreateCustomer(code: "CUST-002", name: "Globex"),
            TestData.CreateCustomer(tenantId: TestData.TenantB, code: "CUST-003", name: "Acme Holdings"));
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var repository = new CustomerReadRepository(database.Context);

        var byName = await repository.ListAsync(TestData.TenantA, 1, 20, "ACME");
        byName.Items.Should().ContainSingle();
        byName.Items.Single().Code.Should().Be("CUST-001");

        var byCode = await repository.ListAsync(TestData.TenantA, 1, 20, "cust-002");
        byCode.Items.Should().ContainSingle();
        byCode.Items.Single().Name.Should().Be("Globex");
    }

    [Fact]
    public async Task CustomerSearch_Should_ReturnAll_WhenSearchIsEmpty()
    {
        await using SqliteCrmDatabase database = await SqliteCrmDatabase.CreateAsync();
        await database.Context.Customers.AddRangeAsync(
            TestData.CreateCustomer(code: "CUST-001"),
            TestData.CreateCustomer(code: "CUST-002", name: "Globex"));
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var repository = new CustomerReadRepository(database.Context);
        var page = await repository.ListAsync(TestData.TenantA, 1, 20, "   ");

        page.TotalCount.Should().Be(2);
        page.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task OrganizationSearch_Should_MatchNameOrTaxIdentifier()
    {
        await using SqliteCrmDatabase database = await SqliteCrmDatabase.CreateAsync();
        Organization first = TestData.CreateOrganization(name: "Holding Group", taxIdentifier: "TAX-999");
        Organization second = TestData.CreateOrganization(name: "Other Org", taxIdentifier: "TAX-111");
        await database.Context.Organizations.AddRangeAsync(first, second);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var repository = new OrganizationReadRepository(database.Context);

        var byName = await repository.ListAsync(TestData.TenantA, 1, 20, "hold");
        byName.Items.Should().ContainSingle();
        byName.Items.Single().Name.Should().Be("Holding Group");

        var byTax = await repository.ListAsync(TestData.TenantA, 1, 20, "tax-111");
        byTax.Items.Should().ContainSingle();
        byTax.Items.Single().Name.Should().Be("Other Org");
    }
}
