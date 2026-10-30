using BSX.Contracts.Permissions;
using BSX.Modules.Crm.Contracts.Permissions;
using BSX.Modules.Identity.Application.Common;
using BSX.Modules.Sales.Contracts.Permissions;

namespace BSX.Modules.Identity.Infrastructure.IntegrationTests.Permissions;

/// <summary>Verifies the platform-wide permission catalog model (ADR-012/ADR-018/ADR-020).</summary>
public sealed class PermissionCatalogTests
{
    [Fact]
    public void Should_DeduplicatePermissions_DeclaredByMultipleSources()
    {
        var catalog = new PermissionCatalog(
        [
            new TestCatalog(new PermissionDefinition("sales.leads.read", "Read leads.", PermissionScope.Tenant)),
            new TestCatalog(new PermissionDefinition("sales.leads.read", "Read leads.", PermissionScope.Tenant)),
        ]);

        catalog.Permissions.Should().ContainSingle()
            .Which.Code.Should().Be("sales.leads.read");
        catalog.Conflicts.Should().BeEmpty();
        catalog.EnsureValid();
    }

    [Fact]
    public void Should_DetectConflictingDescriptions_AndFailValidation()
    {
        var catalog = new PermissionCatalog(
        [
            new TestCatalog(new PermissionDefinition("sales.leads.read", "Read leads.", PermissionScope.Tenant)),
            new TestCatalog(new PermissionDefinition("sales.leads.read", "View leads.", PermissionScope.Tenant)),
        ]);

        catalog.Conflicts.Should().ContainSingle();
        Action act = catalog.EnsureValid;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Should_DetectConflictingScopes_AndFailValidation()
    {
        var catalog = new PermissionCatalog(
        [
            new TestCatalog(new PermissionDefinition("identity.users.read", "Read users.", PermissionScope.Platform)),
            new TestCatalog(new PermissionDefinition("identity.users.read", "Read users.", PermissionScope.Tenant)),
        ]);

        catalog.Conflicts.Should().ContainSingle();
        Action act = catalog.EnsureValid;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Should_ContainTheIdentityCrmAndSalesCatalogs()
    {
        var catalog = new PermissionCatalog(
        [
            new IdentityPermissionCatalog(),
            new CrmPermissionCatalog(),
            new SalesPermissionCatalog(),
        ]);

        string[] codes = catalog.Permissions.Select(permission => permission.Code).ToArray();

        catalog.EnsureValid();
        codes.Should().Contain(["identity.users.read", "crm.customers.read", "sales.opportunities.update"]);
    }

    [Fact]
    public void Should_ClassifyGlobalIdentityPermissionsAsPlatform()
    {
        var catalog = new PermissionCatalog([new IdentityPermissionCatalog()]);

        catalog.ScopeOf("identity.users.read").Should().Be(PermissionScope.Platform);
        catalog.ScopeOf("identity.users.create").Should().Be(PermissionScope.Platform);
        catalog.ScopeOf("identity.users.update").Should().Be(PermissionScope.Platform);
        catalog.ScopeOf("identity.users.manage-status").Should().Be(PermissionScope.Platform);
        catalog.ScopeOf("identity.users.assign-role").Should().Be(PermissionScope.Platform);
    }

    [Fact]
    public void Should_ClassifyTenantConfinedIdentityPermissionsAsTenant()
    {
        var catalog = new PermissionCatalog([new IdentityPermissionCatalog()]);

        catalog.ScopeOf("identity.roles.read").Should().Be(PermissionScope.Tenant);
        catalog.ScopeOf("identity.roles.create").Should().Be(PermissionScope.Tenant);
        catalog.ScopeOf("identity.roles.update").Should().Be(PermissionScope.Tenant);
        catalog.ScopeOf("identity.roles.set-permissions").Should().Be(PermissionScope.Tenant);
        catalog.ScopeOf("identity.permissions.read").Should().Be(PermissionScope.Tenant);
        catalog.ScopeOf("identity.memberships.read").Should().Be(PermissionScope.Tenant);
        catalog.ScopeOf("identity.memberships.manage").Should().Be(PermissionScope.Tenant);
    }

    [Fact]
    public void Should_ClassifyModulePermissionsAsTenant()
    {
        var catalog = new PermissionCatalog(
        [
            new CrmPermissionCatalog(),
            new SalesPermissionCatalog(),
        ]);

        catalog.Permissions.Should().OnlyContain(permission => permission.Scope == PermissionScope.Tenant);
        catalog.ScopeOf("crm.customers.read").Should().Be(PermissionScope.Tenant);
        catalog.ScopeOf("sales.opportunities.update").Should().Be(PermissionScope.Tenant);
    }

    [Fact]
    public void Should_ReturnNullScope_ForUnknownPermission()
    {
        var catalog = new PermissionCatalog([new IdentityPermissionCatalog()]);

        catalog.Contains("crm.orders.read").Should().BeFalse();
        catalog.ScopeOf("crm.orders.read").Should().BeNull();
        catalog.TryGet("crm.orders.read", out PermissionDefinition? definition).Should().BeFalse();
        definition.Should().BeNull();
    }

    private sealed class TestCatalog(params PermissionDefinition[] permissions) : IPermissionCatalogSource
    {
        public IReadOnlyCollection<PermissionDefinition> Permissions { get; } = permissions;
    }
}
