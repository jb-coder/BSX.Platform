using BSX.BuildingBlocks.Cqrs;

namespace BSX.Modules.Identity.Application.Users.UpdateUser;

/// <summary>Updates a user's name and email.</summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="Name">The new display name.</param>
/// <param name="Email">The new email address.</param>
public sealed record UpdateUserCommand(Guid UserId, string Name, string Email) : ICommand;
