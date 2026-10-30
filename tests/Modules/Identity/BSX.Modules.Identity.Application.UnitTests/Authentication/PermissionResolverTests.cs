using BSX.Modules.Identity.Application.Authentication;
using BSX.Modules.Identity.Application.UnitTests.Fakes;
using BSX.Modules.Identity.Domain.Roles;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.UnitTests.Authentication;

/// <summary>
/// Verifies the authorization backstop for permission scope (ADR-020): a tenant role can never
/// contribute a platform permission to a principal, even when the role row is written outside the
/// application write paths.
/// </summary>
public sealed class PermissionResolverTests
{
    private readonly InMemoryRoleRepository _roles = new();

    private PermissionResolver CreateResolver() => new(_roles, TestPermissionCatalog.Platform);

    private static Role CreateRole(TenantId? tenantId, bool isSystem, params string[] codes)
        => Role.Create(
            RoleId.New(),
            tenantId,
            RoleName.Create($"Role-{Guid.NewGuid():N}").Value,
            codes.Select(code => Permission.Create(code).Value),
            isSystem: isSystem).Value;

    [Fact]
    public async Task Should_NotResolvePlatformPermission_FromTenantRole()
    {
        var tenantId = TenantId.New();
        User user = TestData.CreateActiveUser();
        Domain.Memberships.TenantMembership membership = TestData.CreateActiveMembership(tenantId, user.Id);
        Role tenantRole = CreateRole(tenantId, false, "crm.customers.read", "identity.users.read");
        membership.AssignRole(tenantRole.Id);
        _roles.Seed(tenantRole);

        IReadOnlyCollection<string> permissions = await CreateResolver().ResolveAsync(user, membership);

        permissions.Should().BeEquivalentTo(["crm.customers.read"]);
        permissions.Should().NotContain("identity.users.read");
    }

    [Fact]
    public async Task Should_NotResolveUnknownPermission_FromTenantRole()
    {
        var tenantId = TenantId.New();
        User user = TestData.CreateActiveUser();
        Domain.Memberships.TenantMembership membership = TestData.CreateActiveMembership(tenantId, user.Id);
        Role tenantRole = CreateRole(tenantId, false, "crm.customers.read", "crm.orders.read");
        membership.AssignRole(tenantRole.Id);
        _roles.Seed(tenantRole);

        IReadOnlyCollection<string> permissions = await CreateResolver().ResolveAsync(user, membership);

        permissions.Should().BeEquivalentTo(["crm.customers.read"]);
    }

    [Fact]
    public async Task Should_ResolvePlatformPermission_FromPlatformRole()
    {
        var tenantId = TenantId.New();
        User user = TestData.CreateActiveUser();
        Domain.Memberships.TenantMembership membership = TestData.CreateActiveMembership(tenantId, user.Id);
        Role platformRole = CreateRole(null, true, "identity.users.read", "crm.customers.read");
        user.AssignPlatformRole(platformRole.Id);
        _roles.Seed(platformRole);

        IReadOnlyCollection<string> permissions = await CreateResolver().ResolveAsync(user, membership);

        permissions.Should().BeEquivalentTo(["identity.users.read", "crm.customers.read"]);
    }

    [Fact]
    public async Task Should_NotResolveUnknownPermission_FromPlatformRole()
    {
        User user = TestData.CreateActiveUser();
        Role platformRole = CreateRole(null, true, "identity.users.read", "crm.orders.read");
        user.AssignPlatformRole(platformRole.Id);
        _roles.Seed(platformRole);

        IReadOnlyCollection<string> permissions = await CreateResolver().ResolveAsync(user, membership: null);

        permissions.Should().BeEquivalentTo(["identity.users.read"]);
    }

    [Fact]
    public async Task Should_NotResolve_WhenMembershipIsNotActive()
    {
        var tenantId = TenantId.New();
        User user = TestData.CreateActiveUser();
        Domain.Memberships.TenantMembership membership = TestData.CreateActiveMembership(tenantId, user.Id);
        Role tenantRole = CreateRole(tenantId, false, "crm.customers.read");
        membership.AssignRole(tenantRole.Id);
        membership.Remove();
        _roles.Seed(tenantRole);

        IReadOnlyCollection<string> permissions = await CreateResolver().ResolveAsync(user, membership);

        permissions.Should().BeEmpty();
    }
}
