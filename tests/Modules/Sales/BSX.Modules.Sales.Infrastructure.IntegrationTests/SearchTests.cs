using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Domain.Opportunities;
using BSX.Modules.Sales.Infrastructure.IntegrationTests.TestInfrastructure;
using BSX.Modules.Sales.Infrastructure.Repositories;

namespace BSX.Modules.Sales.Infrastructure.IntegrationTests;

public sealed class SearchTests
{
    [Fact]
    public async Task LeadSearch_Should_MatchTitle_CaseInsensitiveAndPartial()
    {
        await using SqliteSalesDatabase database = await SqliteSalesDatabase.CreateAsync();
        await database.Context.Leads.AddRangeAsync(
            TestData.CreateLead(title: "Acme Corporation"),
            TestData.CreateLead(title: "Globex"));
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var repository = new LeadReadRepository(database.Context);

        var byName = await repository.ListAsync(TestData.TenantA, 1, 20, "ACME");
        byName.Items.Should().ContainSingle().Which.Title.Should().Be("Acme Corporation");

        var none = await repository.ListAsync(TestData.TenantA, 1, 20, "missing");
        none.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task OpportunitySearch_Should_MatchTitleAndCustomerId()
    {
        await using SqliteSalesDatabase database = await SqliteSalesDatabase.CreateAsync();
        Guid customerA = Guid.NewGuid();
        Guid customerB = Guid.NewGuid();
        Opportunity first = TestData.CreateOpportunity(customerId: customerA, title: "Acme renewal");
        Opportunity second = TestData.CreateOpportunity(customerId: customerB, title: "Globex expansion");
        await database.Context.Opportunities.AddRangeAsync(first, second);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var repository = new OpportunityReadRepository(database.Context);

        var byTitle = await repository.ListAsync(TestData.TenantA, 1, 20, "acme");
        byTitle.Items.Should().ContainSingle().Which.Id.Should().Be(first.Id.Value);

        var byCustomer = await repository.ListAsync(TestData.TenantA, 1, 20, customerB.ToString());
        byCustomer.Items.Should().ContainSingle().Which.Id.Should().Be(second.Id.Value);

        var none = await repository.ListAsync(TestData.TenantA, 1, 20, Guid.NewGuid().ToString());
        none.Items.Should().BeEmpty();

        var emptyGuid = await repository.ListAsync(TestData.TenantA, 1, 20, Guid.Empty.ToString());
        emptyGuid.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_Should_NotCrossTenantBoundary()
    {
        await using SqliteSalesDatabase database = await SqliteSalesDatabase.CreateAsync();
        Lead tenantALead = TestData.CreateLead(tenantId: TestData.TenantA, title: "Shared title");
        Lead tenantBLead = TestData.CreateLead(tenantId: TestData.TenantB, title: "Shared title");
        await database.Context.Leads.AddRangeAsync(tenantALead, tenantBLead);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var repository = new LeadReadRepository(database.Context);
        var page = await repository.ListAsync(TestData.TenantA, 1, 20, "shared");

        page.Items.Should().ContainSingle();
        page.Items.Single().Id.Should().Be(tenantALead.Id.Value);
    }
}
