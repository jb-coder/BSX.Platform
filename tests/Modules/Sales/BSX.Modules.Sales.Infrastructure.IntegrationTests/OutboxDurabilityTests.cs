using BSX.BuildingBlocks.Persistence.Outbox;
using BSX.Modules.Sales.Domain.DomainEvents;
using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Domain.ValueObjects;
using BSX.Modules.Sales.Infrastructure.IntegrationTests.TestInfrastructure;
using BSX.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Sales.Infrastructure.IntegrationTests;

/// <summary>Verifies that Sales domain events are persisted to its own outbox (ADR-009).</summary>
public sealed class OutboxDurabilityTests
{
    [Fact]
    public async Task Should_PersistLeadCreated_ToTheSalesOutbox()
    {
        await using SqliteSalesDatabase database = await SqliteSalesDatabase.CreateAsync(outbox: true);
        Lead lead = Lead.Create(
            LeadId.New(),
            TestData.Tenant(),
            TestData.LeadTitleOf()).Value;

        await database.Context.Leads.AddAsync(lead);
        await database.Context.SaveChangesAsync();

        LeadCreated domainEvent = lead.DomainEvents.OfType<LeadCreated>().Single();
        OutboxMessage message = await database.Context.OutboxMessages
            .AsNoTracking()
            .SingleAsync(candidate => candidate.EventId == domainEvent.EventId);

        message.Kind.Should().Be(OutboxKinds.Domain);
        message.Status.Should().Be(OutboxMessageStatus.Pending);
        message.EventType.Should().Contain(nameof(LeadCreated));
        message.Payload.Should().Contain(lead.Id.Value.ToString());
    }
}
