using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Domain.Opportunities;
using BSX.Modules.Sales.Infrastructure.IntegrationTests.TestInfrastructure;
using BSX.Modules.Sales.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Sales.Infrastructure.IntegrationTests;

public sealed class ConcurrencyTests
{
    [Fact]
    public async Task Should_PreserveTheWinner_WhenALeadIsModifiedConcurrently()
    {
        await using SqliteSalesDatabase database = await SqliteSalesDatabase.CreateAsync();
        Lead seed = TestData.CreateLead();
        await database.Context.Leads.AddAsync(seed);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        await using SalesDbContext first = database.CreateContext(TestData.TenantA);
        await using SalesDbContext second = database.CreateContext(TestData.TenantA);

        Lead firstCopy = await first.Leads.SingleAsync();
        Lead secondCopy = await second.Leads.SingleAsync();

        firstCopy.ChangeNotes("First").IsSuccess.Should().BeTrue();
        await first.SaveChangesAsync();

        secondCopy.ChangeNotes("Second").IsSuccess.Should().BeTrue();
        var unitOfWork = new SalesUnitOfWork(second, new RecordingDomainEventDispatcher());

        Func<Task> act = () => unitOfWork.SaveChangesAsync();

        await act.Should().ThrowAsync<ConcurrencyConflictException>();

        second.ChangeTracker.Clear();
        Lead persisted = await second.Leads.SingleAsync();
        persisted.Notes.Should().Be("First");
        second.Entry(persisted).Property("Version").CurrentValue.Should().Be(2);
    }

    [Fact]
    public async Task Should_PreserveTheWinner_WhenAnOpportunityIsModifiedConcurrently()
    {
        await using SqliteSalesDatabase database = await SqliteSalesDatabase.CreateAsync();
        Opportunity seed = TestData.CreateOpportunity();
        await database.Context.Opportunities.AddAsync(seed);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        await using SalesDbContext first = database.CreateContext(TestData.TenantA);
        await using SalesDbContext second = database.CreateContext(TestData.TenantA);

        Opportunity firstCopy = await first.Opportunities.SingleAsync();
        Opportunity secondCopy = await second.Opportunities.SingleAsync();

        firstCopy.AdvanceStage().IsSuccess.Should().BeTrue();
        await first.SaveChangesAsync();

        secondCopy.AdvanceStage().IsSuccess.Should().BeTrue();
        var unitOfWork = new SalesUnitOfWork(second, new RecordingDomainEventDispatcher());

        Func<Task> act = () => unitOfWork.SaveChangesAsync();

        await act.Should().ThrowAsync<ConcurrencyConflictException>();

        second.ChangeTracker.Clear();
        Opportunity persisted = await second.Opportunities.SingleAsync();
        persisted.Stage.Should().Be(BSX.Modules.Sales.Domain.Enums.OpportunityStage.Qualification);
    }
}
