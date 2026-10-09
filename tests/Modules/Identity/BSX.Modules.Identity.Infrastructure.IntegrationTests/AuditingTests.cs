using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Infrastructure.IntegrationTests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Identity.Infrastructure.IntegrationTests;

public sealed class AuditingTests
{
    [Fact]
    public async Task Should_StampAuditFields_OnInsert()
    {
        var actor = Guid.NewGuid();
        await using SqliteIdentityDatabase database = await SqliteIdentityDatabase.CreateAsync(new TestCurrentUser(actor));

        User user = TestData.CreateUser();
        database.Context.Users.Add(user);
        await database.Context.SaveChangesAsync();

        var entry = database.Context.Entry(user);
        entry.Property("CreatedOnUtc").CurrentValue.Should().BeOfType<DateTimeOffset>();
        entry.Property("CreatedBy").CurrentValue.Should().Be(actor.ToString());
        entry.Property("UpdatedBy").CurrentValue.Should().Be(actor.ToString());
        entry.Property("IsDeleted").CurrentValue.Should().Be(false);
    }

    [Fact]
    public async Task Should_SoftDelete_AndExcludeFromQueries()
    {
        await using SqliteIdentityDatabase database = await SqliteIdentityDatabase.CreateAsync();
        User user = TestData.CreateUser();
        database.Context.Users.Add(user);
        await database.Context.SaveChangesAsync();

        database.Context.Users.Remove(user);
        await database.Context.SaveChangesAsync();

        int unfiltered = await database.Context.Users.IgnoreQueryFilters().CountAsync();
        unfiltered.Should().Be(1);

        List<User> visible = await database.Context.Users.ToListAsync();
        visible.Should().BeEmpty();

        database.Context.Entry(user).Property("IsDeleted").CurrentValue.Should().Be(true);
        database.Context.Entry(user).Property("DeletedOnUtc").CurrentValue.Should().NotBeNull();
    }
}
