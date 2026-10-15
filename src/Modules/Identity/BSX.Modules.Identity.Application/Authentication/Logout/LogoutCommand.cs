using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;

namespace BSX.Modules.Identity.Application.Authentication.Logout;

/// <summary>Revokes the session owning the presented refresh token.</summary>
/// <param name="RefreshToken">The refresh token of the session to end.</param>
[AllowAnonymous]
public sealed record LogoutCommand(string RefreshToken) : ICommand;
