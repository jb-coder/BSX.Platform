using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Identity.Application.Dtos;

namespace BSX.Modules.Identity.Application.Authentication.Login;

/// <summary>Authenticates a user and starts a session.</summary>
/// <param name="Email">The email address.</param>
/// <param name="Password">The password.</param>
/// <param name="TenantId">The requested tenant, when the user belongs to several.</param>
/// <param name="UserAgent">The client user agent.</param>
/// <param name="IpAddress">The client IP address.</param>
[AllowAnonymous]
public sealed record LoginCommand(
    string Email,
    string Password,
    Guid? TenantId,
    string? UserAgent,
    string? IpAddress) : ICommand<AuthenticationTokensResponse>;
