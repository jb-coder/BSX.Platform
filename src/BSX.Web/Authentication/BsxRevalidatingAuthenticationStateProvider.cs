using System.Security.Claims;
using BSX.Modules.Identity.Application.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.Options;

namespace BSX.Web.Authentication;

/// <summary>
/// Framework-aligned circuit revalidation (ADR-021). It periodically re-reads the session, account,
/// membership and permissions from the Identity store. Invalid sessions are signed out; changed
/// permissions replace the circuit principal so the UI and application services observe them.
/// </summary>
public sealed class BsxRevalidatingAuthenticationStateProvider : RevalidatingServerAuthenticationStateProvider
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WebCurrentUser _currentUser;
    private readonly WebSessionState _sessionState;
    private readonly AuthorizationRevalidationOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BsxRevalidatingAuthenticationStateProvider> _logger;

    /// <summary>Initializes a new instance of the <see cref="BsxRevalidatingAuthenticationStateProvider"/> class.</summary>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="scopeFactory">The scope factory used for the authoritative store read.</param>
    /// <param name="currentUser">The circuit-scoped current user.</param>
    /// <param name="sessionState">The circuit-scoped session state.</param>
    /// <param name="options">The revalidation options.</param>
    /// <param name="timeProvider">The time provider.</param>
    public BsxRevalidatingAuthenticationStateProvider(
        ILoggerFactory loggerFactory,
        IServiceScopeFactory scopeFactory,
        WebCurrentUser currentUser,
        WebSessionState sessionState,
        IOptions<AuthorizationRevalidationOptions> options,
        TimeProvider timeProvider)
        : base(loggerFactory)
    {
        _scopeFactory = scopeFactory;
        _currentUser = currentUser;
        _sessionState = sessionState;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = loggerFactory.CreateLogger<BsxRevalidatingAuthenticationStateProvider>();
    }

    /// <inheritdoc />
    protected override TimeSpan RevalidationInterval => _options.CircuitInterval;

    /// <summary>
    /// Runs a single revalidation immediately. Intended for tests that cannot wait for the circuit
    /// timer.
    /// </summary>
    /// <param name="authenticationState">The state to validate.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    internal Task<bool> ValidateNowAsync(
        AuthenticationState authenticationState,
        CancellationToken cancellationToken = default)
        => ValidateAuthenticationStateAsync(authenticationState, cancellationToken);

    /// <inheritdoc />
    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authenticationState);

        ClaimsPrincipal principal = authenticationState.User;

        if (principal.Identity?.IsAuthenticated != true)
        {
            return true;
        }

        if (!WebPrincipalReader.TryRead(principal, out Guid userId, out Guid sessionId, out Guid? tenantId))
        {
            MarkExpired("The principal has no user or session identifier.");
            return false;
        }

        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            var reader = scope.ServiceProvider.GetRequiredService<IAuthorizationSnapshotReader>();

            AuthorizationSnapshot? snapshot = await reader
                .ReadAsync(userId, sessionId, tenantId, cancellationToken)
                .ConfigureAwait(false);

            if (snapshot is null)
            {
                MarkExpired("The session, account or membership is no longer valid.");
                return false;
            }

            DateTimeOffset accessTokenExpiresAtUtc = WebPrincipalFactory.GetAccessTokenExpiry(principal)
                ?? _timeProvider.GetUtcNow().Add(WebAuthDefaults.SessionLifetime);

            ClaimsPrincipal refreshed = WebPrincipalFactory.Create(snapshot, accessTokenExpiresAtUtc);
            AuthenticationState current = await GetAuthenticationStateAsync().ConfigureAwait(false);

            if (!IsEquivalent(current.User, refreshed))
            {
                SetAuthenticationState(Task.FromResult(new AuthenticationState(refreshed)));
                _currentUser.SetPrincipal(refreshed);
            }

            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Fail closed: an unverifiable circuit is signed out.
            _logger.LogError(exception, "Circuit authorization revalidation failed; signing out the circuit.");
            MarkExpired("Authorization could not be revalidated.");
            return false;
        }
    }

    private void MarkExpired(string reason)
    {
        _sessionState.Expired = true;
        _sessionState.Reason = reason;
    }

    private static bool IsEquivalent(ClaimsPrincipal left, ClaimsPrincipal right)
    {
        if (!string.Equals(
                left.FindFirst(WebAuthDefaults.UserIdClaim)?.Value,
                right.FindFirst(WebAuthDefaults.UserIdClaim)?.Value,
                StringComparison.Ordinal)
            || !string.Equals(
                left.FindFirst(WebAuthDefaults.TenantIdClaim)?.Value,
                right.FindFirst(WebAuthDefaults.TenantIdClaim)?.Value,
                StringComparison.Ordinal)
            || !string.Equals(
                left.FindFirst(WebAuthDefaults.EmailClaim)?.Value,
                right.FindFirst(WebAuthDefaults.EmailClaim)?.Value,
                StringComparison.Ordinal)
            || !string.Equals(
                left.FindFirst(WebAuthDefaults.NameClaim)?.Value,
                right.FindFirst(WebAuthDefaults.NameClaim)?.Value,
                StringComparison.Ordinal))
        {
            return false;
        }

        HashSet<string> leftPermissions = left
            .FindAll(WebAuthDefaults.PermissionClaim)
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.Ordinal);

        HashSet<string> rightPermissions = right
            .FindAll(WebAuthDefaults.PermissionClaim)
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.Ordinal);

        return leftPermissions.SetEquals(rightPermissions);
    }
}
