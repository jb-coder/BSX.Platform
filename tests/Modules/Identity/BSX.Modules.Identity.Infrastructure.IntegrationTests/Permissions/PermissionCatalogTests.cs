using BSX.Contracts.Permissions;
using BSX.Modules.Crm.Contracts.Permissions;
using BSX.Modules.Identity.Application.Common;
using BSX.Modules.Sales.Contracts.Permissions;

namespace BSX.Modules.Identity.Infrastructure.IntegrationTests.Permissions;

/// <summary>Verifies the platform-wide permission catalog model (ADR-012/ADR-018).</summary>
public sealed class PermissionCatalogTests
{
    [Fact]
    public void Should_DeduplicatePermissions_DeclaredByMultipleSources()
    {
        var catalog = new PermissionCatalog(
        [
            new TestCatalog(new PermissionDefinition("sales.leads.read", "Read leads.")),
            new TestCatalog(new PermissionDefinition("sales.leads.read", "Read leads.")),
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
            new TestCatalog(new PermissionDefinition("sales.leads.read", "Read leads.")),
            new TestCatalog(new PermissionDefinition("sales.leads.read", "View leads.")),
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

    private sealed class TestCatalog(params PermissionDefinition[] permissions) : IPermissionCatalogSource
    {
        public IReadOnlyCollection<PermissionDefinition> Permissions { get; } = permissions;
    }
}
