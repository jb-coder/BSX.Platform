using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Infrastructure.IntegrationTests.TestInfrastructure;

namespace BSX.Modules.Sales.Infrastructure.IntegrationTests;

public sealed class AuditingTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Should_StampAuditFields_OnInsertUpdateAndSoftDelete()
    {
        Guid actor = Guid.NewGuid();
        var clock = new MutableTimeProvider(Now);
        await using SqliteSalesDatabase database = await SqliteSalesDatabase.CreateAsync(
            new TestCurrentUser(actor),
            clock);

        Lead lead = TestData.CreateLead();
        await database.Context.Leads.AddAsync(lead);
        await database.Context.SaveChangesAsync();

        var entry = database.Context.Entry(lead);
        entry.Property("CreatedOnUtc").CurrentValue.Should().Be(Now);
        entry.Property("CreatedBy").CurrentValue.Should().Be(actor.ToString());
        entry.Property("UpdatedBy").CurrentValue.Should().Be(actor.ToString());
        entry.Property("IsDeleted").CurrentValue.Should().Be(false);

        clock.Advance(TimeSpan.FromMinutes(5));
        lead.ChangeNotes("Updated").IsSuccess.Should().BeTrue();
        await database.Context.SaveChangesAsync();

        entry.Property("UpdatedOnUtc").CurrentValue.Should().Be(Now.AddMinutes(5));
        entry.Property("UpdatedBy").CurrentValue.Should().Be(actor.ToString());

        clock.Advance(TimeSpan.FromMinutes(5));
        database.Context.Leads.Remove(lead);
        await database.Context.SaveChangesAsync();

        entry.Property("DeletedOnUtc").CurrentValue.Should().Be(Now.AddMinutes(10));
        entry.Property("DeletedBy").CurrentValue.Should().Be(actor.ToString());
    }
}
