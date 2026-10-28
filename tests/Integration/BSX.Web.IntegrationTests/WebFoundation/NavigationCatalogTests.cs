using BSX.Modules.Crm.Contracts.Permissions;
using BSX.Modules.Sales.Contracts.Permissions;
using BSX.Web.Navigation;

namespace BSX.Web.IntegrationTests.WebFoundation;

public sealed class NavigationCatalogTests
{
    [Fact]
    public void For_ShouldHideEverythingExceptAdministration_WhenNoPermissions()
    {
        IReadOnlyList<NavigationSection> sections = NavigationCatalog.For([]);

        sections.Should().ContainSingle();
        sections[0].Label.Should().Be("Administration");
        sections[0].Items.Select(item => item.Label).Should().BeEquivalentTo("Profile", "Sessions");
    }

    [Fact]
    public void For_ShouldHideInaccessibleItems()
    {
        IReadOnlyList<NavigationSection> sections = NavigationCatalog.For([CrmPermissionCodes.CustomersRead]);

        sections.SelectMany(section => section.Items).Select(item => item.Label)
            .Should().BeEquivalentTo("Customers", "Profile", "Sessions");
    }

    [Fact]
    public void For_ShouldShowAllFoundationSections_WhenAllPermissionsAreGranted()
    {
        IReadOnlyList<NavigationSection> sections = NavigationCatalog.For(
        [
            CrmPermissionCodes.CustomersRead,
            CrmPermissionCodes.OrganizationsRead,
            SalesPermissionCodes.LeadsRead,
            SalesPermissionCodes.OpportunitiesRead,
        ]);

        sections.Select(section => section.Label)
            .Should().BeEquivalentTo("CRM", "Sales", "Administration");
        sections.SelectMany(section => section.Items).Should().HaveCount(6);
    }

    [Fact]
    public void Inventory_ShouldNotAppearInNavigation()
    {
        NavigationCatalog.All
            .SelectMany(section => section.Items)
            .Should().NotContain(item => item.Label.Contains("Inventory", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetLandingRoute_ShouldChooseTheFirstAccessibleSection()
    {
        NavigationCatalog.GetLandingRoute([]).Should().Be("/app/admin/profile");
        NavigationCatalog.GetLandingRoute([SalesPermissionCodes.LeadsRead]).Should().Be("/app/sales/leads");
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
    }
}
