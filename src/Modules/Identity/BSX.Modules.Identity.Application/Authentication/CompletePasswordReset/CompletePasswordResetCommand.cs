using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;

namespace BSX.Modules.Identity.Application.Authentication.CompletePasswordReset;

/// <summary>Completes a password reset with a single-use token.</summary>
/// <param name="Token">The plaintext reset token.</param>
/// <param name="NewPassword">The new password.</param>
[AllowAnonymous]
public sealed record CompletePasswordResetCommand(string Token, string NewPassword) : ICommand;
