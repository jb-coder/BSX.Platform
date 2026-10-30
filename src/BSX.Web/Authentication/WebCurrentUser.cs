using System.Security.Claims;
using BSX.BuildingBlocks.Authorization;

namespace BSX.Web.Authentication;

/// <summary>
/// Resolves the current user from the active HTTP request or from the principal supplied by the
/// interactive Blazor circuit. Interactive components call <see cref="SetPrincipal"/> before
/// dispatching application requests so tenant and permission checks remain authoritative.
/// </summary>
public sealed class WebCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private ClaimsPrincipal? _circuitPrincipal;

    /// <summary>Initializes a new instance of the <see cref="WebCurrentUser"/> class.</summary>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    public WebCurrentUser(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    internal ClaimsPrincipal? Principal
        => _circuitPrincipal ?? _httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public Guid? UserId => Guid.TryParse(Principal?.FindFirst(WebAuthDefaults.UserIdClaim)?.Value, out Guid value)
        ? value
        : null;

    /// <inheritdoc />
    public Guid? TenantId => Guid.TryParse(Principal?.FindFirst(WebAuthDefaults.TenantIdClaim)?.Value, out Guid value)
        ? value
        : null;

    /// <inheritdoc />
    public string? Email => Principal?.FindFirst(WebAuthDefaults.EmailClaim)?.Value;

    /// <inheritdoc />
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    /// <inheritdoc />
    public IReadOnlyCollection<string> Permissions => Principal is null
        ? []
        : Principal.FindAll(WebAuthDefaults.PermissionClaim).Select(claim => claim.Value).ToArray();

    /// <summary>Sets the principal for the current interactive circuit.</summary>
    /// <param name="principal">The authenticated or anonymous principal.</param>
    public void SetPrincipal(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        _circuitPrincipal = principal;
    }
}
