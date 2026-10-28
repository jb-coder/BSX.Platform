using BSX.Web.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace BSX.Web.Authorization;

/// <summary>Authorizes a claim carried by the validated access token.</summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        if (context.User.Identity?.IsAuthenticated == true
            && context.User.HasClaim(WebAuthDefaults.PermissionClaim, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
