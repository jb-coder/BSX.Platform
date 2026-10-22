using BSX.Modules.Identity.Domain.Roles;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.Modules.Identity.Infrastructure.IntegrationTests.TestInfrastructure;
using BSX.Modules.Identity.Infrastructure.Permissions;
using BSX.Modules.Sales.Contracts.Permissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BSX.Modules.Identity.Infrastructure.IntegrationTests.Permissions;

/// <summary>
/// Verifies that module permission catalogs can be seeded into the Identity system roles
/// (ADR-018) and that seeding is idempotent.
/// </summary>
public sealed class SystemRolePermissionSynchronizerTests
{
    [Fact]
    public async Task Should_SeedSalesPermissionsIntoAdministratorAndReadPermissionsIntoAuditor()
    {
        await using SqliteIdentityDatabase database = await SqliteIdentityDatabase.CreateAsync();
        var synchronizer = new SystemRolePermissionSynchronizer(
            database.Context,
            [new SalesPermissionCatalog()],
            NullLogger<SystemRolePermissionSynchronizer>.Instance);

        await synchronizer.SynchronizeAsync();

        Role administrator = await GetSystemRoleAsync(database, SystemRolePermissionSynchronizer.AdministratorRoleName);
        Role auditor = await GetSystemRoleAsync(database, SystemRolePermissionSynchronizer.AuditorRoleName);

        string[] allSalesPermissions = new SalesPermissionCatalog()
            .Permissions
            .Select(permission => permission.Code)
            .ToArray();

        administrator.Permissions.Select(permission => permission.Code)
            .Should().BeEquivalentTo(allSalesPermissions);

        auditor.Permissions.Select(permission => permission.Code)
            .Should().BeEquivalentTo(allSalesPermissions.Where(code => code.EndsWith(".read", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Should_BeIdempotent()
    {
        await using SqliteIdentityDatabase database = await SqliteIdentityDatabase.CreateAsync();
        var synchronizer = new SystemRolePermissionSynchronizer(
            database.Context,
            [new SalesPermissionCatalog()],
            NullLogger<SystemRolePermissionSynchronizer>.Instance);

        await synchronizer.SynchronizeAsync();
        List<Role> rolesAfterFirstRun = await database.Context.Roles.ToListAsync();
        int grantsAfterFirstRun = rolesAfterFirstRun.Sum(role => role.Permissions.Count);

        await synchronizer.SynchronizeAsync();

        List<Role> rolesAfterSecondRun = await database.Context.Roles.ToListAsync();
        rolesAfterSecondRun.Should().HaveCount(rolesAfterFirstRun.Count);
        rolesAfterSecondRun.Sum(role => role.Permissions.Count).Should().Be(grantsAfterFirstRun);
    }

    private static Task<Role> GetSystemRoleAsync(SqliteIdentityDatabase database, string name)
    {
        var roleName = RoleName.Create(name).Value;
        return database.Context.Roles
            .SingleAsync(role => role.TenantId == null && role.Name == roleName);
    }
}
