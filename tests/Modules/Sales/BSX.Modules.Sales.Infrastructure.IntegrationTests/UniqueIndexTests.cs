using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Domain.Opportunities;
using BSX.Modules.Sales.Infrastructure.IntegrationTests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Sales.Infrastructure.IntegrationTests;

public sealed class UniqueIndexTests
{
    [Fact]
    public async Task Should_RejectASecondOpportunityForTheSameTenantAndLead()
    {
        await using SqliteSalesDatabase database = await SqliteSalesDatabase.CreateAsync();
        Lead lead = TestData.CreateQualifiedLead();
        Opportunity first = TestData.CreateOpportunity(lead: lead);
        await database.Context.Leads.AddAsync(lead);
        await database.Context.Opportunities.AddAsync(first);
        await database.Context.SaveChangesAsync();

        Opportunity duplicate = TestData.CreateOpportunity(lead: lead);
        await database.Context.Opportunities.AddAsync(duplicate);

        Func<Task> act = () => database.Context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
        (await database.Context.Opportunities.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Should_AllowTheSameLeadIdInDifferentTenants()
    {
        await using SqliteSalesDatabase database = await SqliteSalesDatabase.CreateAsync();
        Guid sharedLeadId = Guid.NewGuid();
        Lead tenantALead = TestData.CreateQualifiedLead(id: sharedLeadId, tenantId: TestData.TenantA);
        Lead tenantBLead = TestData.CreateQualifiedLead(id: sharedLeadId, tenantId: TestData.TenantB);

        await database.Context.Opportunities.AddRangeAsync(
            TestData.CreateOpportunity(lead: tenantALead),
            TestData.CreateOpportunity(lead: tenantBLead));
        await database.Context.SaveChangesAsync();

        (await database.Context.Opportunities.IgnoreQueryFilters().CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Should_AllowRecreatingAnOpportunity_AfterSoftDelete()
    {
        await using SqliteSalesDatabase database = await SqliteSalesDatabase.CreateAsync();
        Lead lead = TestData.CreateQualifiedLead();
        Opportunity first = TestData.CreateOpportunity(lead: lead);
        await database.Context.Leads.AddAsync(lead);
        await database.Context.Opportunities.AddAsync(first);
        await database.Context.SaveChangesAsync();

        database.Context.Opportunities.Remove(first);
        await database.Context.SaveChangesAsync();

        await database.Context.Opportunities.AddAsync(TestData.CreateOpportunity(lead: lead));
        Func<Task> act = () => database.Context.SaveChangesAsync();

        await act.Should().NotThrowAsync();
        (await database.Context.Opportunities.CountAsync()).Should().Be(1);
        (await database.Context.Opportunities.IgnoreQueryFilters().CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Should_NotReportSoftDeletedOpportunityAsExistingForLead()
    {
        await using SqliteSalesDatabase database = await SqliteSalesDatabase.CreateAsync();
        Lead lead = TestData.CreateQualifiedLead();
        Opportunity opportunity = TestData.CreateOpportunity(lead: lead);
        await database.Context.Leads.AddAsync(lead);
        await database.Context.Opportunities.AddAsync(opportunity);
        await database.Context.SaveChangesAsync();

        var repository = new BSX.Modules.Sales.Infrastructure.Repositories.OpportunityRepository(database.Context);
        (await repository.ExistsByLeadIdAsync(TestData.Tenant(), lead.Id)).Should().BeTrue();

        database.Context.Opportunities.Remove(opportunity);
        await database.Context.SaveChangesAsync();

        (await repository.ExistsByLeadIdAsync(TestData.Tenant(), lead.Id)).Should().BeFalse();
    }
}
