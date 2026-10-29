using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Identity.Application.Dtos;

namespace BSX.Modules.Identity.Application.Roles.GetRoleById;

/// <summary>Gets a role by identifier.</summary>
/// <param name="RoleId">The role identifier.</param>
[RequirePermission("identity.roles.read")]
public sealed record GetRoleByIdQuery(Guid RoleId) : IQuery<RoleDto>;
