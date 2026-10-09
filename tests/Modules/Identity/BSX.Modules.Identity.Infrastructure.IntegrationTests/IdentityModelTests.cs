using BSX.Modules.Identity.Infrastructure.IntegrationTests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Identity.Infrastructure.IntegrationTests;

public sealed class IdentityModelTests
{
    [Fact]
    public async Task Should_CreateSchema_ForAllAggregatesAndOutbox()
    {
        await using SqliteIdentityDatabase database = await SqliteIdentityDatabase.CreateAsync();

        var tableNames = database.Context.Model.GetEntityTypes()
            .Select(entityType => entityType.GetTableName())
            .Where(name => name is not null)
            .ToHashSet();

        tableNames.Should().Contain("users");
        tableNames.Should().Contain("roles");
        tableNames.Should().Contain("tenant_memberships");
        tableNames.Should().Contain("service_accounts");
        tableNames.Should().Contain("api_keys");
        tableNames.Should().Contain("credentials");
        tableNames.Should().Contain("outbox_messages");
    }
}
