using System.Security.Claims;
using BSX.BuildingBlocks.Authorization;
using BSX.Web.Authentication;

namespace BSX.Web.IntegrationTests;

/// <summary>
/// Test revalidator that exercises the production revalidation path for principals carrying a real
/// session and keeps legacy claim-only test principals working.
/// </summary>
internal sealed class TestAuthorizationRevalidator : IAuthorizationRevalidator
{
    private readonly WebCurrentUser _currentUser;
    private readonly WebAuthorizationRevalidator _production;

    public TestAuthorizationRevalidator(WebCurrentUser currentUser, WebAuthorizationRevalidator production)
    {
        _currentUser = currentUser;
        _production = production;
    }

    public Task<AuthorizationRevalidationResult> RevalidateAsync(CancellationToken cancellationToken = default)
    {
        ClaimsPrincipal? principal = _currentUser.Principal;

        if (principal?.FindFirst(WebAuthDefaults.SessionIdClaim) is null)
        {
            // Legacy tests build principals directly from claims; there is no session to validate.
            return Task.FromResult(AuthorizationRevalidationResult.Allowed());
        }

        return _production.RevalidateAsync(cancellationToken);
    }
}
