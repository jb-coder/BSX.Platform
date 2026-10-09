namespace BSX.Modules.Identity.Application.Roles.CreateRole;

/// <summary>The result of creating a role.</summary>
/// <param name="RoleId">The new role identifier.</param>
public sealed record CreateRoleResponse(Guid RoleId);
