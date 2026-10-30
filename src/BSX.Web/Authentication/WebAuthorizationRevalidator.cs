using System.Security.Claims;
using BSX.BuildingBlocks.Authorization;
using BSX.Modules.Identity.Application.Authentication;

namespace BSX.Web.Authentication;

/// <summary>
/// Revalidates the current web principal against the Identity store before a request is authorized
/// (ADR-021). The store read runs in a fresh scope so circuit-scoped DbContext tracking cannot hide
/// a revocation.
/// </summary>
internal sealed class WebAuthorizationRevalidator : IAuthorizationRevalidator
{
    private readonly WebCurrentUser _currentUser;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WebAuthorizationRevalidator> _logger;

    /// <summary>Initializes a new instance of the <see cref="WebAuthorizationRevalidator"/> class.</summary>
    /// <param name="currentUser">The current web user.</param>
    /// <param name="scopeFactory">The scope factory used for the authoritative store read.</param>
    /// <param name="logger">The logger.</param>
    public WebAuthorizationRevalidator(
        WebCurrentUser currentUser,
        IServiceScopeFactory scopeFactory,
        ILogger<WebAuthorizationRevalidator> logger)
    {
        _currentUser = currentUser;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AuthorizationRevalidationResult> RevalidateAsync(CancellationToken cancellationToken = default)
    {
        ClaimsPrincipal? principal = _currentUser.Principal;

        if (!WebPrincipalReader.TryRead(principal, out Guid userId, out Guid sessionId, out Guid? tenantId))
        {
            return AuthorizationRevalidationResult.Denied;
        }

        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            var reader = scope.ServiceProvider.GetRequiredService<IAuthorizationSnapshotReader>();

            AuthorizationSnapshot? snapshot = await reader
                .ReadAsync(userId, sessionId, tenantId, cancellationToken)
                .ConfigureAwait(false);

            return snapshot is null
                ? AuthorizationRevalidationResult.Denied
                : AuthorizationRevalidationResult.Authoritative(snapshot.Permissions);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Fail closed: if freshness cannot be verified, the operation is denied.
            _logger.LogError(exception, "Authorization revalidation failed; the request will be denied.");
            return AuthorizationRevalidationResult.Denied;
        }
    }
}
