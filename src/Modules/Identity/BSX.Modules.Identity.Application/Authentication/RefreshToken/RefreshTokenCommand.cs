using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Identity.Application.Dtos;

namespace BSX.Modules.Identity.Application.Authentication.RefreshToken;

/// <summary>Rotates a refresh token and issues new tokens.</summary>
/// <param name="RefreshToken">The presented refresh token.</param>
public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<AuthenticationTokensResponse>;
