using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Application.Memberships.AssignRoleToMembership;
using BSX.Modules.Identity.Application.Roles.SetRolePermissions;
using BSX.Modules.Identity.Domain.Memberships;
using BSX.Modules.Identity.Domain.Roles;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.Modules.Identity.Infrastructure.Persistence;
using BSX.SharedKernel.Results;
using BSX.Web.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BSX.Web.IntegrationTests.Security;

/// <summary>
/// Adversarial coverage for the permission-scope boundary (ADR-020). A tenant administrator must
/// never be able to obtain platform-scoped capabilities through role creation, role editing,
/// membership assignment or direct persistence manipulation.
/// </summary>
public sealed class PermissionScopeBoundaryTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public PermissionScopeBoundaryTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Scenario1_TenantAdmin_CannotCreateRoleWithPlatformPermission()
    {
        HttpClient client = await CreateWorkspaceAdminClientAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/identity/roles",
            new
            {
                name = $"Escalated-{Guid.NewGuid():N}",
                description = "attempted escalation",
                permissions = new[] { "identity.users.read" },
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Scenario2_TenantAdmin_CannotAddPlatformPermissionToExistingRole()
    {
        HttpClient client = await CreateWorkspaceAdminClientAsync();
        Guid roleId = await CreateTenantRoleAsync(client, "crm.customers.read");

        using IServiceScope scope = _factory.Services.CreateScope();
        ISender sender = CreateSender(scope);

        Result result = await sender.SendAsync(
            new SetRolePermissionsCommand(roleId, ["identity.users.read"]));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Permission.ScopeForbidden");
    }

    [Fact]
    public async Task Scenario3_TenantAdmin_CannotPersistUnknownPermission()
    {
        HttpClient client = await CreateWorkspaceAdminClientAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/identity/roles",
            new
            {
                name = $"Unknown-{Guid.NewGuid():N}",
                description = "unknown permission",
                permissions = new[] { "crm.orders.read" },
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Scenario4_PlatformPermissionInjectedDirectly_IsNotEffective()
    {
        Guid tamperedRoleId = await InjectTamperedTenantRoleAsync();

        // The bypass is real: the role is persisted with a platform permission and assigned to the
        // workspace administrator's membership.
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var memberships = scope.ServiceProvider.GetRequiredService<ITenantMembershipRepository>();

            Role tampered = await context.Roles.SingleAsync(role => role.Id == RoleId.From(tamperedRoleId));
            tampered.Permissions.Select(permission => permission.Code).Should().Contain("identity.users.read");

            TenantMembership? membership = await memberships.FindAsync(
                TenantId.From(_factory.TenantId),
                UserId.From(_factory.WorkspaceAdminUserId));
            membership.Should().NotBeNull();
            membership!.RoleIds.Should().Contain(RoleId.From(tamperedRoleId));
        }

        HttpClient client = _factory.CreateClient();
        IdentityApiFactory.LoginTokens tokens = await _factory.LoginAsync(
            client,
            IdentityApiFactory.WorkspaceAdminEmail,
            IdentityApiFactory.AdminPassword,
            _factory.TenantId);

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            var validator = scope.ServiceProvider.GetRequiredService<IAccessTokenValidator>();
            validator.TryValidate(tokens.AccessToken, out ValidatedAccessToken principal).Should().BeTrue();

            principal.Permissions.Should().Contain("crm.customers.read");
            principal.Permissions.Should().NotContain("identity.users.read");
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        HttpResponseMessage users = await client.GetAsync("/identity/users");
        users.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        HttpResponseMessage customers = await client.GetAsync("/crm/customers");
        customers.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Scenario5_TenantAdmin_CannotEnumerateGlobalUsers()
    {
        HttpClient client = await CreateWorkspaceAdminClientAsync();

        HttpResponseMessage response = await client.GetAsync("/identity/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Scenario6_TenantPermissions_ContinueToWork_ForTenantAdmin()
    {
        HttpClient client = await CreateWorkspaceAdminClientAsync();
        string roleName = $"Workspace-{Guid.NewGuid():N}";

        HttpResponseMessage created = await client.PostAsJsonAsync(
            "/identity/roles",
            new
            {
                name = roleName,
                description = "valid tenant role",
                permissions = new[] { "crm.customers.read", "sales.leads.read" },
            });

        created.StatusCode.Should().Be(HttpStatusCode.Created);

        HttpResponseMessage roles = await client.GetAsync("/identity/roles");
        roles.StatusCode.Should().Be(HttpStatusCode.OK);
        (await roles.Content.ReadAsStringAsync()).Should().Contain(roleName);

        HttpResponseMessage customers = await client.GetAsync("/crm/customers");
        customers.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MembershipAssignment_CannotGrantPlatformRole()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        TenantMembership membership = await context.TenantMemberships.SingleAsync(candidate =>
            candidate.UserId == UserId.From(_factory.WorkspaceAdminUserId)
            && candidate.TenantId == TenantId.From(_factory.TenantId));

        Role administrator = await context.Roles.SingleAsync(role =>
            role.TenantId == null
            && role.Name == RoleName.Create("Administrator").Value);

        ISender sender = CreateSender(scope);
        Result result = await sender.SendAsync(
            new AssignRoleToMembershipCommand(membership.Id.Value, administrator.Id.Value));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Role.NotAssignableToMembership");
    }

    [Fact]
    public async Task PlatformAdministrator_RetainsPlatformPermissions()
    {
        HttpClient client = await _factory.CreateAuthenticatedClientAsync(
            IdentityApiFactory.AdminEmail,
            IdentityApiFactory.AdminPassword,
            _factory.TenantId);

        HttpResponseMessage response = await client.GetAsync("/identity/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private Task<HttpClient> CreateWorkspaceAdminClientAsync()
        => _factory.CreateAuthenticatedClientAsync(
            IdentityApiFactory.WorkspaceAdminEmail,
            IdentityApiFactory.AdminPassword,
            _factory.TenantId);

    private static async Task<Guid> CreateTenantRoleAsync(HttpClient client, params string[] permissions)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/identity/roles",
            new
            {
                name = $"Role-{Guid.NewGuid():N}",
                description = "tenant role",
                permissions,
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("roleId").GetGuid();
    }

    private async Task<Guid> InjectTamperedTenantRoleAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var memberships = scope.ServiceProvider.GetRequiredService<ITenantMembershipRepository>();

        Role tampered = Role.Create(
            RoleId.New(),
            TenantId.From(_factory.TenantId),
            RoleName.Create($"Tampered-{Guid.NewGuid():N}").Value,
            [
                Permission.Create("identity.users.read").Value,
                Permission.Create("crm.customers.read").Value,
            ]).Value;

        context.Roles.Add(tampered);

        TenantMembership? membership = await memberships.FindAsync(
            TenantId.From(_factory.TenantId),
            UserId.From(_factory.WorkspaceAdminUserId));

        membership.Should().NotBeNull();
        membership!.AssignRole(tampered.Id);

        await memberships.TrackRoleChangesAsync(membership);
        await context.SaveChangesAsync();

        return tampered.Id.Value;
    }

    private ISender CreateSender(IServiceScope scope)
    {
        var currentUser = scope.ServiceProvider.GetRequiredService<WebCurrentUser>();
        currentUser.SetPrincipal(CreateWorkspaceAdminPrincipal());
        return scope.ServiceProvider.GetRequiredService<ISender>();
    }

    private ClaimsPrincipal CreateWorkspaceAdminPrincipal()
    {
        var claims = new List<Claim>
        {
            new(WebAuthDefaults.UserIdClaim, _factory.WorkspaceAdminUserId.ToString()),
            new(WebAuthDefaults.EmailClaim, IdentityApiFactory.WorkspaceAdminEmail),
            new(WebAuthDefaults.NameClaim, "Workspace Admin"),
            new(WebAuthDefaults.TenantIdClaim, _factory.TenantId.ToString()),
        };

        claims.AddRange(IdentityApiFactory.WorkspaceAdminPermissions.Select(
            permission => new Claim(WebAuthDefaults.PermissionClaim, permission)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "test"));
    }
}
