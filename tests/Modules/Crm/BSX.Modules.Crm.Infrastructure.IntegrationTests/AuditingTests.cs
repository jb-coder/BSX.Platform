using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Infrastructure.IntegrationTests.TestInfrastructure;

namespace BSX.Modules.Crm.Infrastructure.IntegrationTests;

public sealed class AuditingTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Should_StampAuditFields_OnInsert()
    {
        Guid actor = Guid.NewGuid();
        var clock = new MutableTimeProvider(Now);
        await using SqliteCrmDatabase database = await SqliteCrmDatabase.CreateAsync(
            new TestCurrentUser(actor),
            clock);

        Customer customer = TestData.CreateCustomer();
        await database.Context.Customers.AddAsync(customer);
        await database.Context.SaveChangesAsync();

        var entry = database.Context.Entry(customer);
        entry.Property("CreatedOnUtc").CurrentValue.Should().Be(Now);
        entry.Property("CreatedBy").CurrentValue.Should().Be(actor.ToString());
        entry.Property("UpdatedBy").CurrentValue.Should().Be(actor.ToString());
        entry.Property("IsDeleted").CurrentValue.Should().Be(false);
    }

    [Fact]
    public async Task Should_StampUpdatedFields_OnModify()
    {
        Guid actor = Guid.NewGuid();
        var clock = new MutableTimeProvider(Now);
        await using SqliteCrmDatabase database = await SqliteCrmDatabase.CreateAsync(
            new TestCurrentUser(actor),
            clock);

        Customer customer = TestData.CreateCustomer();
        await database.Context.Customers.AddAsync(customer);
        await database.Context.SaveChangesAsync();

        clock.Advance(TimeSpan.FromMinutes(5));
        customer.ChangeName(TestData.Name("Renamed")).IsSuccess.Should().BeTrue();
        await database.Context.SaveChangesAsync();

        var entry = database.Context.Entry(customer);
        entry.Property("CreatedOnUtc").CurrentValue.Should().Be(Now);
        entry.Property("UpdatedOnUtc").CurrentValue.Should().Be(Now.AddMinutes(5));
        entry.Property("UpdatedBy").CurrentValue.Should().Be(actor.ToString());
    }

    [Fact]
    public async Task Should_StampDeletedFields_OnSoftDelete()
    {
        Guid actor = Guid.NewGuid();
        var clock = new MutableTimeProvider(Now);
        await using SqliteCrmDatabase database = await SqliteCrmDatabase.CreateAsync(
            new TestCurrentUser(actor),
            clock);

        Customer customer = TestData.CreateCustomer();
        await database.Context.Customers.AddAsync(customer);
        await database.Context.SaveChangesAsync();

        clock.Advance(TimeSpan.FromMinutes(10));
        database.Context.Customers.Remove(customer);
        await database.Context.SaveChangesAsync();

        var entry = database.Context.Entry(customer);
        entry.Property("DeletedOnUtc").CurrentValue.Should().Be(Now.AddMinutes(10));
        entry.Property("DeletedBy").CurrentValue.Should().Be(actor.ToString());
    }
}
