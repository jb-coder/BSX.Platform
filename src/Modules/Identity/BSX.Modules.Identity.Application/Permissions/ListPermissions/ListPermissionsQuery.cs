using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Identity.Application.Dtos;

namespace BSX.Modules.Identity.Application.Permissions.ListPermissions;

/// <summary>Lists the platform permission catalog.</summary>
[RequirePermission("identity.permissions.read")]
public sealed record ListPermissionsQuery : IQuery<IReadOnlyList<PermissionDto>>;
