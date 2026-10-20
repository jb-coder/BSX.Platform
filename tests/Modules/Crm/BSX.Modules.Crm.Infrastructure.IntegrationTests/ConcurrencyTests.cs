using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Infrastructure.IntegrationTests.TestInfrastructure;
using BSX.Modules.Crm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Crm.Infrastructure.IntegrationTests;

public sealed class ConcurrencyTests
{
    [Fact]
    public async Task Should_DetectConcurrentModification()
    {
        await using SqliteCrmDatabase database = await SqliteCrmDatabase.CreateAsync();
        Customer seed = TestData.CreateCustomer();
        await database.Context.Customers.AddAsync(seed);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        await using CrmDbContext first = database.CreateContext(TestData.TenantA);
        await using CrmDbContext second = database.CreateContext(TestData.TenantA);

        Customer firstCopy = await first.Customers.SingleAsync();
        Customer secondCopy = await second.Customers.SingleAsync();

        firstCopy.ChangeName(TestData.Name("First")).IsSuccess.Should().BeTrue();
        await first.SaveChangesAsync();

        secondCopy.ChangeName(TestData.Name("Second")).IsSuccess.Should().BeTrue();
        var unitOfWork = new CrmUnitOfWork(second, new RecordingDomainEventDispatcher());

        Func<Task> act = async () => await unitOfWork.SaveChangesAsync();

        await act.Should().ThrowAsync<ConcurrencyConflictException>();
    }
}
