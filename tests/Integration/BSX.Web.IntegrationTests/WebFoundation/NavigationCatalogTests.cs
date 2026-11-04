using System.Security.Claims;
using BSX.Modules.Crm.Contracts.Permissions;
using BSX.Modules.Identity.Application.Common;
using BSX.Modules.Sales.Contracts.Permissions;
using BSX.Web.Authentication;
using BSX.Web.Navigation;

namespace BSX.Web.IntegrationTests.WebFoundation;

public sealed class NavigationCatalogTests
{
    private static readonly string[] AllPermissions =
    [
        CrmPermissionCodes.CustomersRead,
        CrmPermissionCodes.OrganizationsRead,
        SalesPermissionCodes.LeadsRead,
        SalesPermissionCodes.OpportunitiesRead,
        IdentityPermissionCodes.UsersRead,
        IdentityPermissionCodes.RolesRead,
        IdentityPermissionCodes.MembershipsRead,
    ];

    [Fact]
    public void For_ShouldAlwaysExposeHome_AndKeepAdministrationSelfService()
    {
        IReadOnlyList<NavigationSection> sections = NavigationCatalog.For([], tenantId: null);

        sections.Select(section => section.Label).Should().BeEquivalentTo("Workspace", "Administration");
        sections.SelectMany(section => section.Items).Select(item => item.Label)
            .Should().BeEquivalentTo("Home", "Sessions");
    }

    [Fact]
    public void For_ShouldHideInaccessibleItems()
    {
        IReadOnlyList<NavigationSection> sections = NavigationCatalog.For(
            [CrmPermissionCodes.CustomersRead],
            Guid.NewGuid());

        sections.SelectMany(section => section.Items).Select(item => item.Label)
            .Should().BeEquivalentTo("Home", "Customers", "Sessions");
    }

    [Fact]
    public void For_ShouldShowAllFoundationSections_WhenAllPermissionsAreGranted()
    {
        IReadOnlyList<NavigationSection> sections = NavigationCatalog.For(
            AllPermissions,
            Guid.NewGuid());

        sections.Select(section => section.Label)
            .Should().BeEquivalentTo("Workspace", "CRM", "Sales", "Administration");
        sections.SelectMany(section => section.Items).Should().HaveCount(9);
    }

    [Fact]
    public void For_ShouldOnlyExposePlatformItems_ForAPlatformOnlyUser()
    {
        IReadOnlyList<NavigationSection> sections = NavigationCatalog.For(
            [IdentityPermissionCodes.UsersRead],
            tenantId: null);

        sections.Select(section => section.Label)
            .Should().BeEquivalentTo("Workspace", "Administration");
        sections.SelectMany(section => section.Items).Select(item => item.Label)
            .Should().BeEquivalentTo("Home", "Users", "Sessions");
        sections.SelectMany(section => section.Items).Select(item => item.Label)
            .Should().NotIntersectWith(["Customers", "Organizations", "Leads", "Opportunities", "Roles", "Memberships"]);
    }

    [Fact]
    public void For_ShouldExposeAllFoundationItems_ForATenantUser()
    {
        IReadOnlyList<NavigationSection> sections = NavigationCatalog.For(
            AllPermissions,
            Guid.NewGuid());

        sections.Select(section => section.Label)
            .Should().BeEquivalentTo("Workspace", "CRM", "Sales", "Administration");
        sections.SelectMany(section => section.Items).Select(item => item.Label)
            .Should().BeEquivalentTo(
                "Home",
                "Customers",
                "Organizations",
                "Leads",
                "Opportunities",
                "Users",
                "Roles",
                "Memberships",
                "Sessions");
    }

    [Fact]
    public void For_ShouldHideTenantScopedPermissions_WhenAPlatformRoleCarriesThemWithoutTenant()
    {
        IReadOnlyList<NavigationSection> sections = NavigationCatalog.For(
        [
            IdentityPermissionCodes.UsersRead,
            IdentityPermissionCodes.RolesRead,
            IdentityPermissionCodes.MembershipsRead,
            CrmPermissionCodes.CustomersRead,
            SalesPermissionCodes.LeadsRead,
        ],
        tenantId: null);

        sections.Select(section => section.Label)
            .Should().BeEquivalentTo("Workspace", "Administration");
        sections.SelectMany(section => section.Items).Select(item => item.Label)
            .Should().BeEquivalentTo("Home", "Users", "Sessions");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void For_ShouldTreatNullAndEmptyTenantAsNoWorkspace(bool useEmptyTenant)
    {
        Guid? tenantId = useEmptyTenant ? Guid.Empty : null;

        IReadOnlyList<NavigationSection> sections = NavigationCatalog.For(
            [CrmPermissionCodes.CustomersRead],
            tenantId);

        sections.SelectMany(section => section.Items).Select(item => item.Label)
            .Should().BeEquivalentTo("Home", "Sessions");
    }

    [Fact]
    public void TenantIdOf_ShouldReadTheClaimAndTreatAnEmptyIdentifierAsNoTenant()
    {
        var tenantId = Guid.NewGuid();

        NavigationCatalog.TenantIdOf(CreatePrincipal(tenantId)).Should().Be(tenantId);
        NavigationCatalog.TenantIdOf(CreatePrincipal(Guid.Empty)).Should().BeNull();
        NavigationCatalog.TenantIdOf(CreatePrincipal(null)).Should().BeNull();
        NavigationCatalog.TenantIdOf(null).Should().BeNull();
    }

    [Fact]
    public void Inventory_ShouldNotAppearInNavigation()
    {
        NavigationCatalog.All
            .SelectMany(section => section.Items)
            .Should().NotContain(item => item.Label.Contains("Inventory", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetLandingRoute_ShouldOpenHome()
    {
        NavigationCatalog.GetLandingRoute([], tenantId: null).Should().Be("/");
        NavigationCatalog.GetLandingRoute([SalesPermissionCodes.LeadsRead], Guid.NewGuid()).Should().Be("/");
    }

    [Fact]
    public void FindByPath_ShouldResolveSectionAndItem()
    {
        NavigationItem? item = NavigationCatalog.FindByPath("/app/crm/customers");
        item.Should().NotBeNull();
        item!.Label.Should().Be("Customers");
        NavigationCatalog.FindSectionLabel(item.Href).Should().Be("CRM");

        NavigationCatalog.FindByPath("/app/crm/customers/00000000-0000-0000-0000-000000000000/edit")
            .Should().Be(item);

        NavigationItem? home = NavigationCatalog.FindByPath("/");
        home.Should().NotBeNull();
        home!.Label.Should().Be("Home");
    }

    private static ClaimsPrincipal CreatePrincipal(Guid? tenantId)
    {
        var claims = new List<Claim>();
        if (tenantId is { } value)
        {
            claims.Add(new Claim(WebAuthDefaults.TenantIdClaim, value.ToString()));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "test"));
    }
}
