using System.Net;
using System.Security.Claims;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Crm.Application.Customers.ListCustomers;
using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Application.Authentication.LogoutAll;
using BSX.Modules.Identity.Application.Memberships.AcceptMembership;
using BSX.Modules.Identity.Application.Memberships.ChangeMembershipRole;
using BSX.Modules.Identity.Application.Memberships.InviteUserToTenant;
using BSX.Modules.Identity.Application.Memberships.ListTenantMemberships;
using BSX.Modules.Identity.Application.Memberships.RemoveMembership;
using BSX.Modules.Identity.Application.Roles.CreateRole;
using BSX.Modules.Identity.Application.Roles.SetRolePermissions;
using BSX.Modules.Identity.Application.Sessions.RevokeSession;
using BSX.Modules.Identity.Application.Users.ActivateUser;
using BSX.Modules.Identity.Application.Users.DeactivateUser;
using BSX.Modules.Identity.Application.Users.RegisterUser;
using BSX.Modules.Sales.Application.Leads.ListLeads;
using BSX.SharedKernel.Results;
using BSX.Web.Authentication;
using BSX.Web.IntegrationTests.WebFoundation;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace BSX.Web.IntegrationTests.Security;

/// <summary>
/// Adversarial coverage for authorization revalidation and access revocation (ADR-021). Each test
/// establishes a real session, revokes access through the Identity application, and then attempts
/// CRM, Sales and administration operations with the original principal.
/// </summary>
public sealed class AuthorizationRevocationTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public AuthorizationRevocationTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task MembershipRemoval_ShouldRevokeCrmSalesAndAdministrationAccess()
    {
        RevocationUser user = await CreateUserAsync(
            "crm.customers.read",
            "sales.leads.read",
            "identity.memberships.read");
        ClaimsPrincipal principal = await SignInAsync(user.Email);

        (await SendAsync(principal, new ListCustomersQuery(1, 20, null))).IsSuccess.Should().BeTrue();
        (await SendAsync(principal, new ListLeadsQuery(1, 20, null))).IsSuccess.Should().BeTrue();
        (await SendAsync(principal, new ListTenantMembershipsQuery())).IsSuccess.Should().BeTrue();

        (await SendAsAdminAsync(new RemoveMembershipCommand(user.MembershipId))).IsSuccess.Should().BeTrue();

        (await SendAsync(principal, new ListCustomersQuery(1, 20, null))).Error.Code
            .Should().Be("Authorization.SessionExpired");
        (await SendAsync(principal, new ListLeadsQuery(1, 20, null))).Error.Code
            .Should().Be("Authorization.SessionExpired");
        (await SendAsync(principal, new ListTenantMembershipsQuery())).Error.Code
            .Should().Be("Authorization.SessionExpired");
    }

    [Fact]
    public async Task RolePermissionChanges_ShouldApplyToOpenSessions()
    {
        RevocationUser user = await CreateUserAsync(
            "crm.customers.read",
            "sales.leads.read",
            "identity.memberships.read");
        ClaimsPrincipal principal = await SignInAsync(user.Email);

        (await SendAsync(principal, new ListCustomersQuery(1, 20, null))).IsSuccess.Should().BeTrue();

        (await SendAsAdminAsync(new SetRolePermissionsCommand(
            user.RoleId,
            ["sales.leads.read", "identity.memberships.read"]))).IsSuccess.Should().BeTrue();

        var customers = await SendAsync(principal, new ListCustomersQuery(1, 20, null));
        customers.IsFailure.Should().BeTrue();
        customers.Error.Code.Should().Be("Authorization.Forbidden");

        // The retained permission continues to work.
        (await SendAsync(principal, new ListLeadsQuery(1, 20, null))).IsSuccess.Should().BeTrue();

        (await SendAsAdminAsync(new SetRolePermissionsCommand(user.RoleId, []))).IsSuccess.Should().BeTrue();

        (await SendAsync(principal, new ListLeadsQuery(1, 20, null))).Error.Code
            .Should().Be("Authorization.Forbidden");
        (await SendAsync(principal, new ListTenantMembershipsQuery())).Error.Code
            .Should().Be("Authorization.Forbidden");
    }

    [Fact]
    public async Task MembershipRoleChange_ShouldApplyToOpenSessions()
    {
        RevocationUser user = await CreateUserAsync("crm.customers.read");
        ClaimsPrincipal principal = await SignInAsync(user.Email);

        (await SendAsync(principal, new ListCustomersQuery(1, 20, null))).IsSuccess.Should().BeTrue();

        Result<CreateRoleResponse> emptyRole = await SendAsAdminAsync(
            new CreateRoleCommand(_factory.TenantId, $"Empty-{Guid.NewGuid():N}", null, []));
        emptyRole.IsSuccess.Should().BeTrue();

        (await SendAsAdminAsync(new ChangeMembershipRoleCommand(
            user.MembershipId,
            emptyRole.Value.RoleId))).IsSuccess.Should().BeTrue();

        (await SendAsync(principal, new ListCustomersQuery(1, 20, null))).Error.Code
            .Should().Be("Authorization.Forbidden");
    }

    [Fact]
    public async Task UserDeactivation_ShouldRevokeAccess()
    {
        RevocationUser user = await CreateUserAsync("crm.customers.read");
        ClaimsPrincipal principal = await SignInAsync(user.Email);

        (await SendAsync(principal, new ListCustomersQuery(1, 20, null))).IsSuccess.Should().BeTrue();

        (await SendAsAdminAsync(new DeactivateUserCommand(user.UserId))).IsSuccess.Should().BeTrue();

        (await SendAsync(principal, new ListCustomersQuery(1, 20, null))).Error.Code
            .Should().Be("Authorization.SessionExpired");
    }

    [Fact]
    public async Task Reactivation_ShouldRequireANewSignIn()
    {
        RevocationUser user = await CreateUserAsync("crm.customers.read");
        ClaimsPrincipal principal = await SignInAsync(user.Email);

        (await SendAsAdminAsync(new DeactivateUserCommand(user.UserId))).IsSuccess.Should().BeTrue();
        (await SendAsAdminAsync(new ActivateUserCommand(user.UserId))).IsSuccess.Should().BeTrue();

        // Reactivation rotates the security stamp, so the pre-existing session is stale.
        (await SendAsync(principal, new ListCustomersQuery(1, 20, null))).Error.Code
            .Should().Be("Authorization.SessionExpired");
    }

    [Fact]
    public async Task SessionRevocation_ShouldRevokeAccess()
    {
        RevocationUser user = await CreateUserAsync("crm.customers.read");
        ClaimsPrincipal principal = await SignInAsync(user.Email);

        (await SendAsync(principal, new ListCustomersQuery(1, 20, null))).IsSuccess.Should().BeTrue();

        Guid sessionId = Guid.Parse(principal.FindFirst(WebAuthDefaults.SessionIdClaim)!.Value);
        (await SendAsync(principal, new RevokeSessionCommand(sessionId))).IsSuccess.Should().BeTrue();

        (await SendAsync(principal, new ListCustomersQuery(1, 20, null))).Error.Code
            .Should().Be("Authorization.SessionExpired");
    }

    [Fact]
    public async Task RevokeAllSessions_ShouldRevokeEveryOpenSession()
    {
        RevocationUser user = await CreateUserAsync("crm.customers.read");
        ClaimsPrincipal first = await SignInAsync(user.Email);
        ClaimsPrincipal second = await SignInAsync(user.Email);

        (await SendAsync(first, new ListCustomersQuery(1, 20, null))).IsSuccess.Should().BeTrue();
        (await SendAsync(second, new ListCustomersQuery(1, 20, null))).IsSuccess.Should().BeTrue();

        (await SendAsync(first, new LogoutAllCommand())).IsSuccess.Should().BeTrue();

        (await SendAsync(first, new ListCustomersQuery(1, 20, null))).Error.Code
            .Should().Be("Authorization.SessionExpired");
        (await SendAsync(second, new ListCustomersQuery(1, 20, null))).Error.Code
            .Should().Be("Authorization.SessionExpired");
    }

    [Fact]
    public async Task CircuitRevalidation_ShouldRefreshPermissionsAndSignOutAfterRevocation()
    {
        RevocationUser user = await CreateUserAsync("crm.customers.read");
        ClaimsPrincipal principal = await SignInAsync(user.Email);

        using IServiceScope scope = _factory.Services.CreateScope();
        var provider = (BsxRevalidatingAuthenticationStateProvider)scope.ServiceProvider
            .GetRequiredService<AuthenticationStateProvider>();
        var sessionState = scope.ServiceProvider.GetRequiredService<WebSessionState>();

        provider.SetAuthenticationState(Task.FromResult(new AuthenticationState(principal)));

        (await provider.ValidateNowAsync(new AuthenticationState(principal))).Should().BeTrue();
        sessionState.Expired.Should().BeFalse();

        // A permission change must replace the circuit principal.
        (await SendAsAdminAsync(new SetRolePermissionsCommand(user.RoleId, []))).IsSuccess.Should().BeTrue();
        (await provider.ValidateNowAsync(new AuthenticationState(principal))).Should().BeTrue();

        AuthenticationState refreshed = await provider.GetAuthenticationStateAsync();
        refreshed.User.HasClaim(WebAuthDefaults.PermissionClaim, "crm.customers.read").Should().BeFalse();

        // A membership removal must terminate the circuit.
        (await SendAsAdminAsync(new RemoveMembershipCommand(user.MembershipId))).IsSuccess.Should().BeTrue();
        (await provider.ValidateNowAsync(new AuthenticationState(principal))).Should().BeFalse();
        sessionState.Expired.Should().BeTrue();
    }

    [Fact]
    public async Task CookieRevalidation_ShouldRedirectToLoginAfterMembershipRemoval()
    {
        RevocationUser user = await CreateUserAsync("crm.customers.read");
        HttpClient client = await WebFoundationTestHelper.SignInAsync(
            _factory,
            user.Email,
            IdentityApiFactory.AdminPassword);

        HttpResponseMessage allowed = await client.GetAsync("/app/crm/customers");
        allowed.StatusCode.Should().Be(HttpStatusCode.OK);

        (await SendAsAdminAsync(new RemoveMembershipCommand(user.MembershipId))).IsSuccess.Should().BeTrue();

        HttpResponseMessage denied = await client.GetAsync("/app/crm/customers");
        denied.StatusCode.Should().Be(HttpStatusCode.Redirect);
        WebFoundationTestHelper.LocationPath(denied).Should().StartWith("/login");
        WebFoundationTestHelper.QueryValue(denied, "expired").Should().Be("true");
    }

    private async Task<RevocationUser> CreateUserAsync(params string[] permissions)
    {
        string email = $"revocation-{Guid.NewGuid():N}@example.com";

        Result<RegisterUserResponse> registered = await SendAsAdminAsync(
            new RegisterUserCommand(email, "Revocation User", IdentityApiFactory.AdminPassword));
        registered.IsSuccess.Should().BeTrue();

        Guid userId = registered.Value.UserId;
        (await SendAsAdminAsync(new ActivateUserCommand(userId))).IsSuccess.Should().BeTrue();

        Result<CreateRoleResponse> role = await SendAsAdminAsync(new CreateRoleCommand(
            _factory.TenantId,
            $"RevocationRole-{Guid.NewGuid():N}",
            null,
            permissions));
        role.IsSuccess.Should().BeTrue();

        Result<InviteUserToTenantResponse> invited = await SendAsAdminAsync(
            new InviteUserToTenantCommand(_factory.TenantId, userId, _factory.AdminUserId));
        invited.IsSuccess.Should().BeTrue();

        (await SendAsAdminAsync(new ChangeMembershipRoleCommand(
            invited.Value.MembershipId,
            role.Value.RoleId))).IsSuccess.Should().BeTrue();

        (await SendAsAdminAsync(new AcceptMembershipCommand(invited.Value.MembershipId)))
            .IsSuccess.Should().BeTrue();

        return new RevocationUser(userId, invited.Value.MembershipId, role.Value.RoleId, email);
    }

    private async Task<ClaimsPrincipal> SignInAsync(string email)
    {
        HttpClient client = _factory.CreateClient();
        IdentityApiFactory.LoginTokens tokens = await _factory.LoginAsync(
            client,
            email,
            IdentityApiFactory.AdminPassword,
            _factory.TenantId);

        using IServiceScope scope = _factory.Services.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<IAccessTokenValidator>();
        validator.TryValidate(tokens.AccessToken, out ValidatedAccessToken validated).Should().BeTrue();

        return WebPrincipalFactory.Create(validated);
    }

    private async Task<T> SendAsync<T>(ClaimsPrincipal principal, IRequest<T> request)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<WebCurrentUser>().SetPrincipal(principal);
        return await scope.ServiceProvider.GetRequiredService<ISender>().SendAsync(request);
    }

    private async Task<T> SendAsAdminAsync<T>(IRequest<T> request)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<WebCurrentUser>().SetPrincipal(CreateAdminPrincipal());
        return await scope.ServiceProvider.GetRequiredService<ISender>().SendAsync(request);
    }

    private ClaimsPrincipal CreateAdminPrincipal()
    {
        var claims = new List<Claim>
        {
            new(WebAuthDefaults.UserIdClaim, _factory.AdminUserId.ToString()),
            new(WebAuthDefaults.EmailClaim, IdentityApiFactory.AdminEmail),
            new(WebAuthDefaults.NameClaim, "Administrator"),
            new(WebAuthDefaults.TenantIdClaim, _factory.TenantId.ToString()),
        };

        claims.AddRange(IdentityApiFactory.AdminPermissions.Select(
            permission => new Claim(WebAuthDefaults.PermissionClaim, permission)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "test"));
    }

    private sealed record RevocationUser(Guid UserId, Guid MembershipId, Guid RoleId, string Email);
}
