using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Domain.DomainEvents;
using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.Modules.Crm.Infrastructure.IntegrationTests.TestInfrastructure;
using BSX.Modules.Crm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Crm.Infrastructure.IntegrationTests;

public sealed class DomainEventDispatchTests
{
    [Fact]
    public async Task Should_CollectPersistAndDispatchDomainEvents_ExactlyOnce()
    {
        await using SqliteCrmDatabase database = await SqliteCrmDatabase.CreateAsync();
        var dispatcher = new RecordingDomainEventDispatcher();
        var unitOfWork = new CrmUnitOfWork(database.Context, dispatcher);

        Customer customer = Customer.Create(
            CustomerId.New(),
            TestData.Tenant(),
            TestData.Code("CUST-001"),
            TestData.Name()).Value;
        await database.Context.Customers.AddAsync(customer);

        await unitOfWork.SaveChangesAndDispatchAsync();

        (await database.Context.Customers.CountAsync()).Should().Be(1);
        dispatcher.Dispatched.Should().ContainSingle(domainEvent => domainEvent is CustomerCreated);
        customer.DomainEvents.Should().BeEmpty();

        await unitOfWork.SaveChangesAndDispatchAsync();

        dispatcher.Dispatched.Should().ContainSingle();
    }
}
