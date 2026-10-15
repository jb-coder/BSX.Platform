namespace BSX.Modules.Identity.Application.Dtos;

/// <summary>The tokens returned by login and refresh.</summary>
public sealed record AuthenticationTokensResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken);
