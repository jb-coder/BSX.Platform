using System.Security.Claims;
using BSX.Modules.Crm.Contracts.Permissions;
using BSX.Modules.Identity.Application.Common;
using BSX.Modules.Sales.Contracts.Permissions;
using BSX.Web.Authentication;
using BSX.Web.Components.Layout;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BSX.Web.IntegrationTests.WebFoundation;

public sealed class NavMenuComponentTests
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
    public async Task NavMenu_ShouldHideTenantScopedItems_WhenThePrincipalHasNoTenant()
    {
        ClaimsPrincipal principal = CreatePrincipal(tenantId: null, AllPermissions);

        string html = await RenderNavMenuAsync(principal);

        html.Should().Contain("Home");
        html.Should().Contain("/app/admin/users");
        html.Should().Contain("/app/admin/sessions");
        html.Should().NotContain("/app/crm/customers");
        html.Should().NotContain("/app/crm/organizations");
        html.Should().NotContain("/app/sales/leads");
        html.Should().NotContain("/app/sales/opportunities");
        html.Should().NotContain("/app/admin/roles");
        html.Should().NotContain("/app/admin/memberships");
    }

    [Fact]
    public async Task NavMenu_ShouldShowTenantScopedItems_WhenThePrincipalHasATenant()
    {
        ClaimsPrincipal principal = CreatePrincipal(Guid.NewGuid(), AllPermissions);

        string html = await RenderNavMenuAsync(principal);

        html.Should().Contain("/app/crm/customers");
        html.Should().Contain("/app/crm/organizations");
        html.Should().Contain("/app/sales/leads");
        html.Should().Contain("/app/sales/opportunities");
        html.Should().Contain("/app/admin/users");
        html.Should().Contain("/app/admin/roles");
        html.Should().Contain("/app/admin/memberships");
        html.Should().Contain("/app/admin/sessions");
    }

    private static async Task<string> RenderNavMenuAsync(ClaimsPrincipal principal)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<AuthenticationStateProvider>(new TestAuthenticationStateProvider(principal));
        services.AddSingleton<NavigationManager, TestNavigationManager>();

        await using ServiceProvider provider = services.BuildServiceProvider();
        ILoggerFactory loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        await using var renderer = new HtmlRenderer(provider, loggerFactory);

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = await renderer.RenderComponentAsync<NavMenu>();
            await root.QuiescenceTask;
            return root.ToHtmlString();
        });
    }

    private static ClaimsPrincipal CreatePrincipal(Guid? tenantId, params string[] permissions)
    {
        var claims = new List<Claim>();

        if (tenantId is { } value)
        {
            claims.Add(new Claim(WebAuthDefaults.TenantIdClaim, value.ToString()));
        }

        foreach (string permission in permissions)
        {
            claims.Add(new Claim(WebAuthDefaults.PermissionClaim, permission));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "test"));
    }

    private sealed class TestAuthenticationStateProvider(ClaimsPrincipal principal)
        : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(principal));
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("https://localhost/", "https://localhost/");

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
        }
    }
}
