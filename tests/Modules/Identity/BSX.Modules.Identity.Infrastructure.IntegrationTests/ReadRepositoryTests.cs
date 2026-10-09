using BSX.Modules.Identity.Application.Dtos;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.Modules.Identity.Infrastructure.IntegrationTests.TestInfrastructure;
using BSX.Modules.Identity.Infrastructure.Repositories;

namespace BSX.Modules.Identity.Infrastructure.IntegrationTests;

public sealed class ReadRepositoryTests
{
    [Fact]
    public async Task Should_SearchUsers_WithPaging()
    {
        await using SqliteIdentityDatabase database = await SqliteIdentityDatabase.CreateAsync();
        for (int index = 0; index < 3; index++)
        {
            database.Context.Users.Add(TestData.CreateUser($"user{index}@example.com"));
        }

        await database.Context.SaveChangesAsync();

        var repository = new UserReadRepository(database.Context);

        PagedResult<UserSummaryDto> page = await repository.SearchAsync(1, 2, null);
        PagedResult<UserSummaryDto> filtered = await repository.SearchAsync(1, 10, "user1");

        page.TotalCount.Should().Be(3);
        page.Items.Should().HaveCount(2);
        filtered.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_ListRolesForTenant()
    {
        await using SqliteIdentityDatabase database = await SqliteIdentityDatabase.CreateAsync();
        var tenantId = TenantId.New();
        database.Context.Roles.Add(TestData.CreateRole(tenantId, "Manager", "crm.customers.read"));
        database.Context.Roles.Add(TestData.CreateRole(tenantId, "Sales", "crm.customers.create"));
        await database.Context.SaveChangesAsync();

        var repository = new RoleReadRepository(database.Context);

        IReadOnlyList<RoleDto> roles = await repository.ListAsync(tenantId);

        roles.Should().HaveCount(2);
        roles.Should().Contain(role => role.Permissions.Contains("crm.customers.read"));
    }
}
