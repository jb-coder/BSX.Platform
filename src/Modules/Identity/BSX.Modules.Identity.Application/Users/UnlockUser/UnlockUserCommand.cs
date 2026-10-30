using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;

namespace BSX.Modules.Identity.Application.Users.UnlockUser;

/// <summary>Clears an account lockout (ADR-022).</summary>
/// <param name="UserId">The user identifier.</param>
[RequirePermission("identity.users.manage-status")]
public sealed record UnlockUserCommand(Guid UserId) : ICommand;
