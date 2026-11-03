using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;

namespace BSX.Modules.Identity.Application.Users.SetUserPlatformRoles;

/// <summary>Replaces a user's platform (system) roles.</summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="RoleIds">The complete set of platform role identifiers to assign.</param>
[RequirePermission("identity.users.assign-role")]
public sealed record SetUserPlatformRolesCommand(
    Guid UserId,
    IReadOnlyCollection<Guid> RoleIds) : ICommand;
