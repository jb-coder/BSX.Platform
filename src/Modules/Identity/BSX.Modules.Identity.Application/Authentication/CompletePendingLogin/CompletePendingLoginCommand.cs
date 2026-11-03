using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Identity.Application.Dtos;

namespace BSX.Modules.Identity.Application.Authentication.CompletePendingLogin;

/// <summary>
/// Completes a verified sign-in after the user selects a workspace. The pending-login secret is
/// validated server-side; the non-secret selection handle prevents a mismatched chooser submission.
/// </summary>
/// <param name="PendingLoginToken">The secret pending-login token from the HttpOnly cookie.</param>
/// <param name="SelectionId">The non-secret selection handle shown in the chooser.</param>
/// <param name="TenantId">The selected tenant.</param>
/// <param name="UserAgent">The client user agent.</param>
/// <param name="IpAddress">The client IP address.</param>
[AllowAnonymous]
public sealed record CompletePendingLoginCommand(
    string PendingLoginToken,
    string SelectionId,
    Guid TenantId,
    string? UserAgent,
    string? IpAddress) : ICommand<AuthenticationTokensResponse>;
