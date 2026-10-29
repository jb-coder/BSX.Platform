using System.Security.Claims;
using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Identity.Application.Authentication.Login;
using BSX.Modules.Identity.Application.Authentication.LogoutAll;
using BSX.Modules.Identity.Application.Dtos;
using BSX.Modules.Identity.Application.Memberships.AcceptMembership;
using BSX.Modules.Identity.Application.Memberships.ChangeMembershipRole;
using BSX.Modules.Identity.Application.Memberships.InviteUserToTenant;
using BSX.Modules.Identity.Application.Memberships.ListTenantMemberships;
using BSX.Modules.Identity.Application.Memberships.RemoveMembership;
using BSX.Modules.Identity.Application.Permissions.ListPermissions;
using BSX.Modules.Identity.Application.Roles.CreateRole;
using BSX.Modules.Identity.Application.Roles.GetRoleById;
using BSX.Modules.Identity.Application.Roles.ListRoles;
using BSX.Modules.Identity.Application.Roles.SetRolePermissions;
using BSX.Modules.Identity.Application.Roles.UpdateRole;
using BSX.Modules.Identity.Application.Sessions.ListMySessions;
using BSX.Modules.Identity.Application.Sessions.RevokeSession;
using BSX.Modules.Identity.Application.Users.ActivateUser;
using BSX.Modules.Identity.Application.Users.DeactivateUser;
using BSX.Modules.Identity.Application.Users.GetUserById;
using BSX.Modules.Identity.Application.Users.RegisterUser;
using BSX.Modules.Identity.Application.Users.UpdateUser;
using BSX.SharedKernel.Results;
using BSX.Web.Administration;
using BSX.Web.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace BSX.Web.IntegrationTests.WebFoundation;

public sealed class AdministrationWebWorkflowTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public AdministrationWebWorkflowTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task UserLifecycle_ShouldCreateActivateUpdateAndDeactivate()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISender sender = CreateSender(scope);
        string email = $"managed-{Guid.NewGuid():N}@example.com";

        Result<RegisterUserResponse> created = await sender.SendAsync(
            new RegisterUserCommand(email, "Managed User", "Passw0rd!"));

        created.IsSuccess.Should().BeTrue();
        UserDto user = (await sender.SendAsync(new GetUserByIdQuery(created.Value.UserId))).Value;
        user.Status.Should().Be("Pending");

        (await sender.SendAsync(new ActivateUserCommand(user.Id))).IsSuccess.Should().BeTrue();
        (await sender.SendAsync(new UpdateUserCommand(
            user.Id,
            "Managed User Renamed",
            $"renamed-{Guid.NewGuid():N}@example.com"))).IsSuccess.Should().BeTrue();
        (await sender.SendAsync(new DeactivateUserCommand(user.Id))).IsSuccess.Should().BeTrue();

        UserDto final = (await sender.SendAsync(new GetUserByIdQuery(user.Id))).Value;
        final.Status.Should().Be("Disabled");
        final.Name.Should().Be("Managed User Renamed");
    }

    [Fact]
    public async Task RoleLifecycle_ShouldCreateUpdatePermissionsAndProtectSystemRoles()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISender sender = CreateSender(scope);
        string name = $"Role-{Guid.NewGuid():N}";

        Result<CreateRoleResponse> created = await sender.SendAsync(new CreateRoleCommand(
            _factory.TenantId,
            name,
            "Initial description",
            ["crm.customers.read"]));

        created.IsSuccess.Should().BeTrue();

        Result<RoleDto> createdRole = await sender.SendAsync(new GetRoleByIdQuery(created.Value.RoleId));
        createdRole.Value.Name.Should().Be(name);
        createdRole.Value.Permissions.Should().Contain("crm.customers.read");

        (await sender.SendAsync(new UpdateRoleCommand(
            created.Value.RoleId,
            $"{name}-updated",
            "Updated description"))).IsSuccess.Should().BeTrue();

        (await sender.SendAsync(new SetRolePermissionsCommand(
            created.Value.RoleId,
            ["crm.organizations.read", "sales.leads.read"]))).IsSuccess.Should().BeTrue();

        RoleDto updated = (await sender.SendAsync(new GetRoleByIdQuery(created.Value.RoleId))).Value;
        updated.Name.Should().EndWith("-updated");
        updated.Description.Should().Be("Updated description");
        updated.Permissions.Should().BeEquivalentTo(["crm.organizations.read", "sales.leads.read"]);

        RoleDto administrator = (await sender.SendAsync(new ListRolesQuery(null))).Value
            .Single(role => role.Name == "Administrator");

        Result renameSystem = await sender.SendAsync(new UpdateRoleCommand(
            administrator.Id,
            "Renamed Administrator",
            null));

        renameSystem.IsFailure.Should().BeTrue();
        renameSystem.Error.Code.Should().Be("Identity.Role.SystemImmutable");

        Result changeSystemPermissions = await sender.SendAsync(
            new SetRolePermissionsCommand(administrator.Id, ["crm.customers.read"]));

        changeSystemPermissions.IsFailure.Should().BeTrue();
        changeSystemPermissions.Error.Code.Should().Be("Identity.Role.SystemImmutable");

        IReadOnlyList<PermissionDto> catalog = (await sender.SendAsync(new ListPermissionsQuery())).Value;
        catalog.Should().Contain(permission => permission.Code == "identity.memberships.manage");
    }

    [Fact]
    public async Task MembershipLifecycle_ShouldInviteChangeRoleAcceptAndRemove()
    {
        string email = $"member-{Guid.NewGuid():N}@example.com";

        Result<RegisterUserResponse> user = await SendAsync(
            new RegisterUserCommand(email, "Workspace Member", "Passw0rd!"));
        Result<CreateRoleResponse> role = await SendAsync(new CreateRoleCommand(
            _factory.TenantId,
            $"MemberRole-{Guid.NewGuid():N}",
            null,
            []));

        Result<InviteUserToTenantResponse> invited = await SendAsync(
            new InviteUserToTenantCommand(_factory.TenantId, user.Value.UserId, _factory.AdminUserId));

        invited.IsSuccess.Should().BeTrue();

        MembershipDto membership = (await SendAsync(new ListTenantMembershipsQuery())).Value
            .Single(candidate => candidate.Id == invited.Value.MembershipId);
        membership.Status.Should().Be("Invited");

        (await SendAsync(new ChangeMembershipRoleCommand(membership.Id, role.Value.RoleId)))
            .IsSuccess.Should().BeTrue();

        MembershipDto changed = (await SendAsync(new ListTenantMembershipsQuery())).Value
            .Single(candidate => candidate.Id == membership.Id);
        changed.RoleIds.Should().Contain(role.Value.RoleId);

        (await SendAsync(new AcceptMembershipCommand(membership.Id))).IsSuccess.Should().BeTrue();

        MembershipDto active = (await SendAsync(new ListTenantMembershipsQuery())).Value
            .Single(candidate => candidate.Id == membership.Id);
        active.Status.Should().Be("Active");
        active.RoleIds.Should().Contain(role.Value.RoleId);

        (await SendAsync(new RemoveMembershipCommand(membership.Id))).IsSuccess.Should().BeTrue();

        MembershipDto removed = (await SendAsync(new ListTenantMembershipsQuery())).Value
            .Single(candidate => candidate.Id == membership.Id);
        removed.Status.Should().Be("Removed");
        removed.RoleIds.Should().BeEmpty();
    }

    [Fact]
    public async Task SessionLifecycle_ShouldListRevokeAndRevokeAll()
    {
        string firstAgent = $"AdminWorkflow-{Guid.NewGuid():N}";
        string secondAgent = $"AdminWorkflow-{Guid.NewGuid():N}";

        Result<AuthenticationTokensResponse> first = await SendAsync(new LoginCommand(
            IdentityApiFactory.AdminEmail,
            IdentityApiFactory.AdminPassword,
            _factory.TenantId,
            firstAgent,
            "127.0.0.1"));

        Result<AuthenticationTokensResponse> second = await SendAsync(new LoginCommand(
            IdentityApiFactory.AdminEmail,
            IdentityApiFactory.AdminPassword,
            _factory.TenantId,
            secondAgent,
            "127.0.0.1"));

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();

        IReadOnlyList<SessionDto> sessions = (await SendAsync(new ListMySessionsQuery())).Value;
        SessionDto firstSession = sessions.Single(session => session.UserAgent == firstAgent);

        (await SendAsync(new RevokeSessionCommand(firstSession.Id))).IsSuccess.Should().BeTrue();

        SessionDto revoked = (await SendAsync(new ListMySessionsQuery())).Value
            .Single(session => session.Id == firstSession.Id);
        revoked.Status.Should().Be("Revoked");

        (await SendAsync(new LogoutAllCommand())).IsSuccess.Should().BeTrue();

        IReadOnlyList<SessionDto> afterLogoutAll = (await SendAsync(new ListMySessionsQuery())).Value;
        afterLogoutAll.Should().NotContain(session => session.Status == "Active");
    }

    [Fact]
    public async Task IdentityWebResults_ShouldMapConcurrencyToStableConflict()
    {
        Result result = await IdentityWebResults.ExecuteAsync(
            () => Task.FromException<Result>(new ConcurrencyConflictException("stale write")));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Concurrency.Conflict");
    }

    private async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISender sender = CreateSender(scope);
        return await sender.SendAsync(request);
    }

    private ISender CreateSender(IServiceScope scope)
    {
        var currentUser = scope.ServiceProvider.GetRequiredService<WebCurrentUser>();
        currentUser.SetPrincipal(CreatePrincipal());
        return scope.ServiceProvider.GetRequiredService<ISender>();
    }

    private ClaimsPrincipal CreatePrincipal()
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
}
