using BSX.BuildingBlocks.Cqrs;

namespace BSX.Modules.Identity.Application.Users.DeactivateUser;

/// <summary>Deactivates a user.</summary>
/// <param name="UserId">The user identifier.</param>
public sealed record DeactivateUserCommand(Guid UserId) : ICommand;
