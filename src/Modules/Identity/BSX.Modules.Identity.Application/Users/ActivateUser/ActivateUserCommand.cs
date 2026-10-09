using BSX.BuildingBlocks.Cqrs;

namespace BSX.Modules.Identity.Application.Users.ActivateUser;

/// <summary>Activates a user.</summary>
/// <param name="UserId">The user identifier.</param>
public sealed record ActivateUserCommand(Guid UserId) : ICommand;
