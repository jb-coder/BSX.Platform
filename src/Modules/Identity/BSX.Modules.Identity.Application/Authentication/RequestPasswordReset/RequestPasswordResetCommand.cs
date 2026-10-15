using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;

namespace BSX.Modules.Identity.Application.Authentication.RequestPasswordReset;

/// <summary>Requests a password reset. Always succeeds (enumeration-safe).</summary>
/// <param name="Email">The account email.</param>
[AllowAnonymous]
public sealed record RequestPasswordResetCommand(string Email) : ICommand<RequestPasswordResetResponse>;
