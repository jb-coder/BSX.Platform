using BSX.Contracts.Permissions;
using BSX.Modules.Crm.Contracts.Permissions;
using BSX.Modules.Identity.Application.Common;
using BSX.Modules.Sales.Contracts.Permissions;

namespace BSX.Modules.Identity.Application.UnitTests;

/// <summary>The real platform permission catalog (Identity, CRM and Sales) for unit tests.</summary>
internal static class TestPermissionCatalog
{
    internal static PermissionCatalog Platform { get; } = new(
    [
        new IdentityPermissionCatalog(),
        new CrmPermissionCatalog(),
        new SalesPermissionCatalog(),
    ]);
}
