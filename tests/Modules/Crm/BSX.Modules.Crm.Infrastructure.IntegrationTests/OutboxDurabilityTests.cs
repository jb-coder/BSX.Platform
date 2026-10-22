using BSX.BuildingBlocks.Persistence.Outbox;
using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Domain.DomainEvents;
using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.Modules.Crm.Infrastructure.IntegrationTests.TestInfrastructure;
using BSX.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Crm.Infrastructure.IntegrationTests;

/// <summary>Verifies that CRM domain events are persisted to its own outbox (ADR-009).</summary>
public sealed class OutboxDurabilityTests
{
    [Fact]
    public async Task Should_PersistCustomerCreated_ToTheCrmOutbox()
    {
        await using SqliteCrmDatabase database = await SqliteCrmDatabase.CreateAsync(outbox: true);
        Customer customer = Customer.Create(
            CustomerId.New(),
            TestData.Tenant(),
            TestData.Code(),
            TestData.Name()).Value;

        await database.Context.Customers.AddAsync(customer);
        await database.Context.SaveChangesAsync();

        CustomerCreated domainEvent = customer.DomainEvents.OfType<CustomerCreated>().Single();
        OutboxMessage message = await database.Context.OutboxMessages
            .AsNoTracking()
            .SingleAsync(candidate => candidate.EventId == domainEvent.EventId);

        message.Kind.Should().Be(OutboxKinds.Domain);
        message.Status.Should().Be(OutboxMessageStatus.Pending);
        message.EventType.Should().Contain(nameof(CustomerCreated));
        message.Payload.Should().Contain(customer.Id.Value.ToString());
    }
}
