using BSX.Modules.Sales.Domain.DomainEvents;
using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Domain.Opportunities;
using BSX.Modules.Sales.Domain.ValueObjects;
using BSX.Modules.Sales.Infrastructure.IntegrationTests.TestInfrastructure;
using BSX.Modules.Sales.Infrastructure.Persistence;

namespace BSX.Modules.Sales.Infrastructure.IntegrationTests;

public sealed class DomainEventDispatchTests
{
    [Fact]
    public async Task Should_CollectPersistAndDispatchDomainEvents_ExactlyOnce()
    {
        await using SqliteSalesDatabase database = await SqliteSalesDatabase.CreateAsync();
        var dispatcher = new RecordingDomainEventDispatcher();
        var unitOfWork = new SalesUnitOfWork(database.Context, dispatcher);

        Lead lead = Lead.Create(
            LeadId.New(),
            TestData.Tenant(),
            TestData.LeadTitleOf(),
            TestData.Customer()).Value;
        await database.Context.Leads.AddAsync(lead);

        await unitOfWork.SaveChangesAndDispatchAsync();

        dispatcher.Dispatched.OfType<LeadCreated>().Should().ContainSingle();
        lead.DomainEvents.Should().BeEmpty();

        lead.MarkContacted().IsSuccess.Should().BeTrue();
        lead.Qualify().IsSuccess.Should().BeTrue();
        Opportunity opportunity = Opportunity.CreateFromLead(
            OpportunityId.New(),
            TestData.Tenant(),
            lead,
            TestData.Customer(),
            TestData.OpportunityTitleOf(),
            TestData.MoneyOf(),
            TestData.ProbabilityOf(),
            TestData.CloseDateOf()).Value;

        await database.Context.Opportunities.AddAsync(opportunity);
        await unitOfWork.SaveChangesAndDispatchAsync();

        dispatcher.Dispatched.OfType<LeadQualified>().Should().ContainSingle();
        dispatcher.Dispatched.OfType<OpportunityCreated>().Should().ContainSingle();
        lead.DomainEvents.Should().BeEmpty();
        opportunity.DomainEvents.Should().BeEmpty();

        await unitOfWork.SaveChangesAndDispatchAsync();

        dispatcher.Dispatched.OfType<LeadCreated>().Should().ContainSingle();
        dispatcher.Dispatched.OfType<LeadQualified>().Should().ContainSingle();
        dispatcher.Dispatched.OfType<OpportunityCreated>().Should().ContainSingle();
    }
}
