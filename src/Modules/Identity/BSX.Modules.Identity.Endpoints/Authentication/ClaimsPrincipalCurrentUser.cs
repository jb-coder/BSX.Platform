using System.Security.Claims;
using BSX.BuildingBlocks.Authorization;
using Microsoft.AspNetCore.Http;

namespace BSX.Modules.Identity.Endpoints.Authentication;

/// <summary>Maps the authenticated <see cref="ClaimsPrincipal"/> to <see cref="ICurrentUser"/>.</summary>
internal sealed class ClaimsPrincipalCurrentUser : ICurrentUser
{
    /// <summary>Claim type for a granted permission.</summary>
    public const string PermissionClaim = "permissions";

    /// <summary>Claim type for the active tenant.</summary>
    public const string TenantIdClaim = "tenant_id";

    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Initializes a new instance of the <see cref="ClaimsPrincipalCurrentUser"/> class.</summary>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    public ClaimsPrincipalCurrentUser(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public Guid? UserId => Guid.TryParse(Principal?.FindFirst("sub")?.Value, out Guid value) ? value : null;

    /// <inheritdoc />
    public Guid? TenantId => Guid.TryParse(Principal?.FindFirst(TenantIdClaim)?.Value, out Guid value) ? value : null;

    /// <inheritdoc />
    public string? Email => Principal?.FindFirst("email")?.Value;

    /// <inheritdoc />
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    /// <inheritdoc />
    public IReadOnlyCollection<string> Permissions => Principal is null
        ? []
        : Principal.FindAll(PermissionClaim).Select(claim => claim.Value).ToArray();
}
