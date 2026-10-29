using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;

namespace BSX.Modules.Identity.Application.Roles.UpdateRole;

/// <summary>Updates a non-system role's name and description.</summary>
/// <param name="RoleId">The role identifier.</param>
/// <param name="Name">The new role name.</param>
/// <param name="Description">The new optional description.</param>
[RequirePermission("identity.roles.update")]
public sealed record UpdateRoleCommand(Guid RoleId, string Name, string? Description) : ICommand;
