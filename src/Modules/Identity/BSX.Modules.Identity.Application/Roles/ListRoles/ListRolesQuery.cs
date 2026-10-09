using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Identity.Application.Dtos;

namespace BSX.Modules.Identity.Application.Roles.ListRoles;

/// <summary>Lists roles; null tenant lists system roles.</summary>
/// <param name="TenantId">The tenant identifier, or null for system roles.</param>
public sealed record ListRolesQuery(Guid? TenantId) : IQuery<IReadOnlyList<RoleDto>>;
