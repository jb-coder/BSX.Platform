using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Identity.Application.Dtos;

namespace BSX.Modules.Identity.Application.Users.GetUserById;

/// <summary>Gets a user by identifier.</summary>
/// <param name="UserId">The user identifier.</param>
[RequirePermission("identity.users.read")]
public sealed record GetUserByIdQuery(Guid UserId) : IQuery<UserDto>;
