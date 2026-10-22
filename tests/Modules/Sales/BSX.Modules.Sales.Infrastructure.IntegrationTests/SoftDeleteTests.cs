using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Domain.Opportunities;
using BSX.Modules.Sales.Infrastructure.IntegrationTests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Sales.Infrastructure.IntegrationTests;

public sealed class SoftDeleteTests
{
    [Fact]
    public async Task Should_SoftDeleteLead_AndExcludeItFromQueries()
    {
        await using SqliteSalesDatabase database = await SqliteSalesDatabase.CreateAsync();
        Lead lead = TestData.CreateLead();
        await database.Context.Leads.AddAsync(lead);
        await database.Context.SaveChangesAsync();

        database.Context.Leads.Remove(lead);
        await database.Context.SaveChangesAsync();

        (await database.Context.Leads.CountAsync()).Should().Be(0);
        (await database.Context.Leads.IgnoreQueryFilters().CountAsync()).Should().Be(1);
        database.Context.Entry(lead).Property("IsDeleted").CurrentValue.Should().Be(true);
        database.Context.Entry(lead).Property("DeletedOnUtc").CurrentValue.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_SoftDeleteOpportunity_AndExcludeItFromQueries()
    {
        await using SqliteSalesDatabase database = await SqliteSalesDatabase.CreateAsync();
        Opportunity opportunity = TestData.CreateOpportunity();
        await database.Context.Opportunities.AddAsync(opportunity);
        await database.Context.SaveChangesAsync();

        database.Context.Opportunities.Remove(opportunity);
        await database.Context.SaveChangesAsync();

        (await database.Context.Opportunities.CountAsync()).Should().Be(0);
        (await database.Context.Opportunities.IgnoreQueryFilters().CountAsync()).Should().Be(1);
        database.Context.Entry(opportunity).Property("IsDeleted").CurrentValue.Should().Be(true);
    }
}
