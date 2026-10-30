using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Identity.Application.Authentication.CompletePasswordReset;
using BSX.Modules.Identity.Application.Authentication.DiscoverTenantOptions;
using BSX.Modules.Identity.Application.Authentication.Login;
using BSX.Modules.Identity.Application.Authentication.Logout;
using BSX.Modules.Identity.Application.Authentication.LogoutAll;
using BSX.Modules.Identity.Application.Authentication.RefreshToken;
using BSX.Modules.Identity.Application.Authentication.RequestPasswordReset;
using BSX.Modules.Identity.Application.Memberships.InviteUserToTenant;
using BSX.Modules.Identity.Application.Roles.CreateRole;
using BSX.Modules.Identity.Application.Roles.ListRoles;
using BSX.Modules.Identity.Application.Sessions.ListMySessions;
using BSX.Modules.Identity.Application.Sessions.RevokeSession;
using BSX.Modules.Identity.Application.Users.GetUserById;
using BSX.Modules.Identity.Application.Users.ListUsers;
using BSX.Modules.Identity.Application.Users.RegisterUser;
using BSX.Modules.Identity.Application.Users.UnlockUser;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BSX.Modules.Identity.Endpoints;

/// <summary>Maps the Identity HTTP endpoints (thin transport adapters over the CQRS handlers).</summary>
public static class IdentityEndpointRouteBuilderExtensions
{
    /// <summary>Maps all Identity endpoints under <c>/identity</c>.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The same endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder group = endpoints.MapGroup("/identity").WithTags("Identity");

        MapAuthentication(group);
        MapSessions(group);
        MapAdministration(group);

        return endpoints;
    }

    private static void MapAuthentication(RouteGroupBuilder group)
    {
        group.MapPost("/login", async (LoginRequest request, ISender sender, HttpContext http, CancellationToken cancellationToken) =>
        {
            var command = new LoginCommand(
                request.Email,
                request.Password,
                request.TenantId,
                http.Request.Headers.UserAgent.ToString(),
                http.Connection.RemoteIpAddress?.ToString());

            return IdentityResults.From(await sender.SendAsync(command, cancellationToken));
        }).AllowAnonymous();

        group.MapPost("/login/tenants", async (
            TenantDiscoveryRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            IdentityResults.From(await sender.SendAsync(
                new DiscoverTenantOptionsCommand(request.Email, request.Password),
                cancellationToken)))
            .AllowAnonymous();

        group.MapPost("/refresh", async (RefreshRequest request, ISender sender, CancellationToken cancellationToken) =>
            IdentityResults.From(await sender.SendAsync(new RefreshTokenCommand(request.RefreshToken), cancellationToken)))
            .AllowAnonymous();

        group.MapPost("/password-reset/request", async (PasswordResetRequestRequest request, ISender sender, CancellationToken cancellationToken) =>
            IdentityResults.From(await sender.SendAsync(new RequestPasswordResetCommand(request.Email), cancellationToken)))
            .AllowAnonymous();

        group.MapPost("/password-reset/complete", async (CompletePasswordResetRequest request, ISender sender, CancellationToken cancellationToken) =>
            IdentityResults.From(await sender.SendAsync(new CompletePasswordResetCommand(request.Token, request.NewPassword), cancellationToken)))
            .AllowAnonymous();

        group.MapPost("/logout", async (LogoutRequest request, ISender sender, CancellationToken cancellationToken) =>
            IdentityResults.From(await sender.SendAsync(new LogoutCommand(request.RefreshToken), cancellationToken)))
            .AllowAnonymous();

        group.MapPost("/logout-all", async (ISender sender, CancellationToken cancellationToken) =>
            IdentityResults.From(await sender.SendAsync(new LogoutAllCommand(), cancellationToken)))
            .RequireAuthorization();
    }

    private static void MapSessions(RouteGroupBuilder group)
    {
        group.MapGet("/sessions", async (ISender sender, CancellationToken cancellationToken) =>
            IdentityResults.From(await sender.SendAsync(new ListMySessionsQuery(), cancellationToken)))
            .RequireAuthorization();

        group.MapDelete("/sessions/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            IdentityResults.From(await sender.SendAsync(new RevokeSessionCommand(id), cancellationToken)))
            .RequireAuthorization();
    }

    private static void MapAdministration(RouteGroupBuilder group)
    {
        RouteGroupBuilder users = group.MapGroup("/users").RequireAuthorization();

        users.MapGet("/", async (ISender sender, CancellationToken cancellationToken, int page = 1, int size = 20, string? search = null) =>
            IdentityResults.From(await sender.SendAsync(new ListUsersQuery(page, size, search), cancellationToken)));

        users.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            IdentityResults.From(await sender.SendAsync(new GetUserByIdQuery(id), cancellationToken)));

        users.MapPost("/", async (CreateUserRequest request, ISender sender, CancellationToken cancellationToken) =>
            IdentityResults.FromCreated(await sender.SendAsync(new RegisterUserCommand(request.Email, request.Name, request.Password), cancellationToken)));

        users.MapPost("/{id:guid}/unlock", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            IdentityResults.From(await sender.SendAsync(new UnlockUserCommand(id), cancellationToken)));

        RouteGroupBuilder roles = group.MapGroup("/roles").RequireAuthorization();

        roles.MapGet("/", async (ISender sender, ITenantContext tenant, CancellationToken cancellationToken) =>
            IdentityResults.From(await sender.SendAsync(new ListRolesQuery(tenant.TenantId), cancellationToken)));

        roles.MapPost("/", async (CreateRoleRequest request, ISender sender, ITenantContext tenant, CancellationToken cancellationToken) =>
            IdentityResults.FromCreated(await sender.SendAsync(
                new CreateRoleCommand(tenant.TenantId, request.Name, request.Description, request.Permissions),
                cancellationToken)));

        group.MapPost("/memberships", async (InviteMemberRequest request, ISender sender, ITenantContext tenant, CancellationToken cancellationToken) =>
            IdentityResults.FromCreated(await sender.SendAsync(
                new InviteUserToTenantCommand(tenant.TenantId ?? Guid.Empty, request.UserId, request.InvitedByUserId),
                cancellationToken)))
            .RequireAuthorization();
    }

    /// <summary>Login request.</summary>
    public sealed record LoginRequest(string Email, string Password, Guid? TenantId);

    /// <summary>Tenant discovery request used before selecting a workspace.</summary>
    public sealed record TenantDiscoveryRequest(string Email, string Password);

    /// <summary>Refresh request.</summary>
    public sealed record RefreshRequest(string RefreshToken);

    /// <summary>Password reset request.</summary>
    public sealed record PasswordResetRequestRequest(string Email);

    /// <summary>Password reset completion request.</summary>
    public sealed record CompletePasswordResetRequest(string Token, string NewPassword);

    /// <summary>Logout request.</summary>
    public sealed record LogoutRequest(string RefreshToken);

    /// <summary>Create user request.</summary>
    public sealed record CreateUserRequest(string Email, string Name, string? Password);

    /// <summary>Create role request.</summary>
    public sealed record CreateRoleRequest(string Name, string? Description, IReadOnlyCollection<string> Permissions);

    /// <summary>Invite member request.</summary>
    public sealed record InviteMemberRequest(Guid UserId, Guid? InvitedByUserId);
}
