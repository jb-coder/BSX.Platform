using System.Security.Claims;
using BSX.Modules.Identity.Application.Abstractions;
using BSX.Web.Authentication;
using BSX.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace BSX.Web.IntegrationTests.WebFoundation;

public sealed class PermissionAuthorizationTests
{
    [Fact]
    public async Task Handler_ShouldSucceed_WhenPermissionClaimExists()
    {
        ClaimsPrincipal user = UserWithPermissions("crm.customers.read");
        var requirement = new PermissionRequirement("crm.customers.read");
        var context = new AuthorizationHandlerContext([requirement], user, resource: null);

        await new PermissionAuthorizationHandler().HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handler_ShouldNotSucceed_WhenPermissionIsMissing()
    {
        ClaimsPrincipal user = UserWithPermissions("sales.leads.read");
        var requirement = new PermissionRequirement("crm.customers.read");
        var context = new AuthorizationHandlerContext([requirement], user, resource: null);

        await new PermissionAuthorizationHandler().HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Handler_ShouldNotSucceed_WhenUserIsAnonymous()
    {
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        var requirement = new PermissionRequirement("crm.customers.read");
        var context = new AuthorizationHandlerContext([requirement], anonymous, resource: null);

        await new PermissionAuthorizationHandler().HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task PolicyProvider_ShouldBuildPermissionPolicies()
    {
        var provider = new PermissionPolicyProvider(
            Options.Create(new AuthorizationOptions()));

        AuthorizationPolicy? policy = await provider.GetPolicyAsync("Permission:crm.customers.read");

        policy.Should().NotBeNull();
        PermissionRequirement requirement = policy!.Requirements.OfType<PermissionRequirement>().Should().ContainSingle().Subject;
        requirement.Permission.Should().Be("crm.customers.read");
    }

    [Fact]
    public async Task PolicyProvider_ShouldDelegateUnknownPolicies()
    {
        var provider = new PermissionPolicyProvider(Options.Create(new AuthorizationOptions()));

        (await provider.GetPolicyAsync("AnyOtherPolicy")).Should().BeNull();
    }

    [Fact]
    public void PrincipalFactory_ShouldCopyIdentityTenantAndPermissions()
    {
        var token = new ValidatedAccessToken(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "User",
            "admin@example.com",
            "Admin User",
            ["crm.customers.read"],
            "stamp",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddMinutes(15));

        ClaimsPrincipal principal = WebPrincipalFactory.Create(token);

        principal.Identity!.IsAuthenticated.Should().BeTrue();
        principal.Identity.Name.Should().Be("Admin User");
        principal.FindFirst(WebAuthDefaults.EmailClaim)!.Value.Should().Be("admin@example.com");
        principal.FindFirst(WebAuthDefaults.TenantIdClaim)!.Value.Should().Be(token.TenantId!.Value.ToString());
        principal.HasClaim(WebAuthDefaults.PermissionClaim, "crm.customers.read").Should().BeTrue();
    }

    private static ClaimsPrincipal UserWithPermissions(params string[] permissions)
    {
        var claims = permissions.Select(permission => new Claim(WebAuthDefaults.PermissionClaim, permission));
        return new ClaimsPrincipal(new ClaimsIdentity(
            claims,
            authenticationType: WebAuthDefaults.CookieScheme,
            nameType: WebAuthDefaults.NameClaim,
            roleType: ClaimTypes.Role));
    }
}
