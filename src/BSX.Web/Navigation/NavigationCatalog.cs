using BSX.Modules.Crm.Contracts.Permissions;
using BSX.Modules.Identity.Application.Common;
using BSX.Modules.Sales.Contracts.Permissions;

namespace BSX.Web.Navigation;

/// <summary>
/// The single source of truth for application navigation. Items are filtered by permission before
/// they reach the shell; the Inventory module is deliberately not visible in this foundation.
/// </summary>
public static class NavigationCatalog
{
    /// <summary>The application route prefix.</summary>
    public const string AppRoot = "/app";

    private static readonly NavigationSection[] Sections =
    [
        new("Workspace",
        [
            new(
                "Home",
                "/",
                null,
                "Workspace overview and first-run guidance.",
                "home"),
        ]),
        new("CRM",
        [
            new(
                "Customers",
                $"{AppRoot}/crm/customers",
                CrmPermissionCodes.CustomersRead,
                "Customer records, contacts and addresses.",
                "customers"),
            new(
                "Organizations",
                $"{AppRoot}/crm/organizations",
                CrmPermissionCodes.OrganizationsRead,
                "Organizations and their billing details.",
                "organizations"),
        ]),
        new("Sales",
        [
            new(
                "Leads",
                $"{AppRoot}/sales/leads",
                SalesPermissionCodes.LeadsRead,
                "Incoming leads and qualification.",
                "leads"),
            new(
                "Opportunities",
                $"{AppRoot}/sales/opportunities",
                SalesPermissionCodes.OpportunitiesRead,
                "Revenue opportunities and their lifecycle.",
                "opportunities"),
        ]),
        new("Administration",
        [
            new(
                "Users",
                $"{AppRoot}/admin/users",
                IdentityPermissionCodes.UsersRead,
                "User accounts and lifecycle.",
                "users"),
            new(
                "Roles",
                $"{AppRoot}/admin/roles",
                IdentityPermissionCodes.RolesRead,
                "Roles and permission assignments.",
                "roles"),
            new(
                "Memberships",
                $"{AppRoot}/admin/memberships",
                IdentityPermissionCodes.MembershipsRead,
                "Workspace members and invitations.",
                "memberships"),
            new(
                "Sessions",
                $"{AppRoot}/admin/sessions",
                null,
                "Devices and sessions with access to your account.",
                "sessions"),
        ]),
    ];

    /// <summary>Gets every section, including items the user cannot access.</summary>
    public static IReadOnlyList<NavigationSection> All => Sections;

    /// <summary>Returns the sections the user may see, removing empty sections.</summary>
    /// <param name="permissions">The user's granted permissions.</param>
    public static IReadOnlyList<NavigationSection> For(IReadOnlyCollection<string> permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);

        var granted = new HashSet<string>(permissions, StringComparer.Ordinal);
        var sections = new List<NavigationSection>();

        foreach (NavigationSection section in Sections)
        {
            NavigationItem[] visible = section.Items
                .Where(item => item.Permission is null || granted.Contains(item.Permission))
                .ToArray();

            if (visible.Length > 0)
            {
                sections.Add(new NavigationSection(section.Label, visible));
            }
        }

        return sections;
    }

    /// <summary>Finds the first destination a user can access.</summary>
    /// <param name="permissions">The user's granted permissions.</param>
    public static string GetLandingRoute(IReadOnlyCollection<string> permissions)
        => For(permissions).SelectMany(section => section.Items).FirstOrDefault()?.Href
            ?? $"{AppRoot}/admin/profile";

    /// <summary>Finds the navigation item for a path.</summary>
    /// <param name="path">The application-relative path.</param>
    public static NavigationItem? FindByPath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        string normalized = path.Split('?', '#')[0].TrimEnd('/');
        if (normalized.Length == 0)
        {
            normalized = "/";
        }

        return Sections
            .SelectMany(section => section.Items)
            .FirstOrDefault(item =>
                string.Equals(item.Href, normalized, StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith($"{item.Href}/", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Returns the section label for an item, when known.</summary>
    /// <param name="href">The item URL.</param>
    public static string? FindSectionLabel(string href)
        => Sections
            .FirstOrDefault(section => section.Items.Any(item => string.Equals(item.Href, href, StringComparison.OrdinalIgnoreCase)))
            ?.Label;

    /// <summary>Gets permissions from a principal's claims.</summary>
    /// <param name="principal">The authenticated principal.</param>
    public static IReadOnlyCollection<string> PermissionsOf(System.Security.Claims.ClaimsPrincipal? principal)
        => principal?.FindAll(BSX.Web.Authentication.WebAuthDefaults.PermissionClaim)
            .Select(claim => claim.Value)
            .ToArray()
            ?? [];
}
