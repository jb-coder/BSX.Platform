using BSX.Modules.Sales.Application.Dtos;
using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Domain.Opportunities;
using BSX.Modules.Sales.Infrastructure.IntegrationTests.TestInfrastructure;
using BSX.Modules.Sales.Infrastructure.Repositories;

namespace BSX.Modules.Sales.Infrastructure.IntegrationTests;

public sealed class ReadModelTests
{
    [Fact]
    public async Task Should_ProjectLeadDetail_AndListSummaries()
    {
        await using SqliteSalesDatabase database = await SqliteSalesDatabase.CreateAsync();
        Guid customerId = Guid.NewGuid();
        Lead lead = TestData.CreateLead(customerId: customerId, notes: "Notes");
        await database.Context.Leads.AddAsync(lead);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var repository = new LeadReadRepository(database.Context);

        LeadDto? detail = await repository.GetByIdAsync(TestData.TenantA, lead.Id.Value);
        detail.Should().NotBeNull();
        detail!.Title.Should().Be("Enterprise deal");
        detail.Status.Should().Be("New");
        detail.CustomerId.Should().Be(customerId);
        detail.Notes.Should().Be("Notes");

        var page = await repository.ListAsync(TestData.TenantA, 1, 20, null);
        page.TotalCount.Should().Be(1);
        page.Items.Should().ContainSingle();
        page.Items.Single().Title.Should().Be("Enterprise deal");
    }

    [Fact]
    public async Task Should_ProjectOpportunityDetail_AndWeightedAmount()
    {
        await using SqliteSalesDatabase database = await SqliteSalesDatabase.CreateAsync();
        Guid customerId = Guid.NewGuid();
        Lead lead = TestData.CreateQualifiedLead(customerId: customerId);
        Opportunity opportunity = TestData.CreateOpportunity(
            lead: lead,
            customerId: customerId,
            amount: 10000m,
            probability: 33);
        await database.Context.Leads.AddAsync(lead);
        await database.Context.Opportunities.AddAsync(opportunity);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var repository = new OpportunityReadRepository(database.Context);
        OpportunityDto? detail = await repository.GetByIdAsync(TestData.TenantA, opportunity.Id.Value);

        detail.Should().NotBeNull();
        detail!.LeadId.Should().Be(lead.Id.Value);
        detail.CustomerId.Should().Be(customerId);
        detail.Amount.Should().Be(10000m);
        detail.Currency.Should().Be("USD");
        detail.Probability.Should().Be(33);
        detail.Stage.Should().Be("Discovery");
        detail.WeightedAmount.Should().Be(3300m);
    }

    [Fact]
    public async Task Should_PageOpportunitySummaries()
    {
        await using SqliteSalesDatabase database = await SqliteSalesDatabase.CreateAsync();
        for (int index = 0; index < 3; index++)
        {
            await database.Context.Opportunities.AddAsync(
                TestData.CreateOpportunity(title: $"Opportunity {index}"));
        }

        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var repository = new OpportunityReadRepository(database.Context);
        PagedResult<OpportunitySummaryDto> page = await repository.ListAsync(TestData.TenantA, 1, 2, null);

        page.TotalCount.Should().Be(3);
        page.Items.Should().HaveCount(2);
        page.Page.Should().Be(1);
        page.Size.Should().Be(2);
    }
}
