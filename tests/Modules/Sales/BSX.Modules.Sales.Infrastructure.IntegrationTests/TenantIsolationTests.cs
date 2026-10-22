using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Domain.Opportunities;
using BSX.Modules.Sales.Infrastructure.IntegrationTests.TestInfrastructure;
using BSX.Modules.Sales.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Sales.Infrastructure.IntegrationTests;

public sealed class TenantIsolationTests
{
    [Fact]
    public async Task LeadFilters_Should_BeFailClosed_AndSystemScopeShouldSeeAll()
    {
        await using SqliteSalesDatabase database = await SqliteSalesDatabase.CreateAsync();
        Lead tenantALead = TestData.CreateLead(tenantId: TestData.TenantA, title: "Alpha lead");
        Lead tenantBLead = TestData.CreateLead(tenantId: TestData.TenantB, title: "Beta lead");
        await database.Context.Leads.AddRangeAsync(tenantALead, tenantBLead);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        await using var tenantA = database.CreateContext(TestData.TenantA);
        await using var missing = database.CreateContext(null);
        await using var system = database.CreateContext(null, isSystem: true);

        (await tenantA.Leads.CountAsync()).Should().Be(1);
        (await tenantA.Leads.SingleAsync()).Title.Value.Should().Be("Alpha lead");
        (await missing.Leads.CountAsync()).Should().Be(0);

        (await system.Leads.CountAsync(lead =>
            lead.Title == TestData.LeadTitleOf("Alpha lead")
            || lead.Title == TestData.LeadTitleOf("Beta lead"))).Should().Be(2);
    }

    [Fact]
    public async Task OpportunityFilters_Should_BeFailClosed_AndSystemScopeShouldSeeAll()
    {
        await using SqliteSalesDatabase database = await SqliteSalesDatabase.CreateAsync();
        Opportunity tenantA = TestData.CreateOpportunity(tenantId: TestData.TenantA, title: "Alpha opportunity");
        Opportunity tenantB = TestData.CreateOpportunity(tenantId: TestData.TenantB, title: "Beta opportunity");
        await database.Context.Opportunities.AddRangeAsync(tenantA, tenantB);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        await using var scoped = database.CreateContext(TestData.TenantA);
        await using var missing = database.CreateContext(null);
        await using var system = database.CreateContext(null, isSystem: true);

        (await scoped.Opportunities.CountAsync()).Should().Be(1);
        (await scoped.Opportunities.SingleAsync()).Title.Value.Should().Be("Alpha opportunity");
        (await missing.Opportunities.CountAsync()).Should().Be(0);
        (await system.Opportunities.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task ReadRepositories_Should_NotCrossTenantBoundary()
    {
        await using SqliteSalesDatabase database = await SqliteSalesDatabase.CreateAsync();
        Lead tenantALead = TestData.CreateLead(tenantId: TestData.TenantA, title: "Alpha lead");
        Opportunity tenantAOpportunity = TestData.CreateOpportunity(
            tenantId: TestData.TenantA,
            title: "Alpha opportunity");
        Opportunity tenantBOpportunity = TestData.CreateOpportunity(
            tenantId: TestData.TenantB,
            title: "Beta opportunity");
        await database.Context.Leads.AddAsync(tenantALead);
        await database.Context.Opportunities.AddRangeAsync(tenantAOpportunity, tenantBOpportunity);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        await using var tenantA = database.CreateContext(TestData.TenantA);
        var leads = new LeadReadRepository(tenantA);
        var opportunities = new OpportunityReadRepository(tenantA);

        (await leads.GetByIdAsync(TestData.TenantA, tenantALead.Id.Value)).Should().NotBeNull();
        (await opportunities.GetByIdAsync(TestData.TenantB, tenantBOpportunity.Id.Value)).Should().BeNull();
        (await opportunities.ListAsync(TestData.TenantA, 1, 20, null)).Items.Should().ContainSingle();
    }

    [Fact]
    public async Task WriteRepositories_Should_NotCrossTenantBoundary()
    {
        await using SqliteSalesDatabase database = await SqliteSalesDatabase.CreateAsync();
        Lead lead = TestData.CreateLead(tenantId: TestData.TenantA);
        Opportunity opportunity = TestData.CreateOpportunity(tenantId: TestData.TenantA);
        await database.Context.Leads.AddAsync(lead);
        await database.Context.Opportunities.AddAsync(opportunity);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        await using var tenantB = database.CreateContext(TestData.TenantB);
        var leadRepository = new LeadRepository(tenantB);
        var opportunityRepository = new OpportunityRepository(tenantB);

        (await leadRepository.GetByIdAsync(TestData.Tenant(TestData.TenantB), lead.Id)).Should().BeNull();
        (await opportunityRepository.GetByIdAsync(
            TestData.Tenant(TestData.TenantB),
            opportunity.Id)).Should().BeNull();
    }
}
