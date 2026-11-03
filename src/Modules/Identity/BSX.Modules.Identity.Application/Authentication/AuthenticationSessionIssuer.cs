using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Application.Dtos;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.Memberships;
using BSX.Modules.Identity.Domain.Sessions;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Authentication;

/// <summary>
/// Starts an authenticated session and issues the access and refresh tokens for a user that has
/// already been authenticated (password login or a verified pending-workspace selection).
/// </summary>
public sealed class AuthenticationSessionIssuer
{
    private const int MaxActiveSessions = 10;
    private const string UserPrincipalType = "User";

    private readonly IUserSessionRepository _sessions;
    private readonly PermissionResolver _permissionResolver;
    private readonly ISecureTokenGenerator _tokenGenerator;
    private readonly IRefreshTokenHasher _refreshTokenHasher;
    private readonly IAccessTokenIssuer _accessTokenIssuer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="AuthenticationSessionIssuer"/> class.</summary>
    /// <param name="sessions">The session repository.</param>
    /// <param name="permissionResolver">The effective permission resolver.</param>
    /// <param name="tokenGenerator">The secure token generator.</param>
    /// <param name="refreshTokenHasher">The refresh-token hasher.</param>
    /// <param name="accessTokenIssuer">The access-token issuer.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <param name="timeProvider">The time provider.</param>
    public AuthenticationSessionIssuer(
        IUserSessionRepository sessions,
        PermissionResolver permissionResolver,
        ISecureTokenGenerator tokenGenerator,
        IRefreshTokenHasher refreshTokenHasher,
        IAccessTokenIssuer accessTokenIssuer,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _sessions = sessions;
        _permissionResolver = permissionResolver;
        _tokenGenerator = tokenGenerator;
        _refreshTokenHasher = refreshTokenHasher;
        _accessTokenIssuer = accessTokenIssuer;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    /// <summary>Issues a session and tokens for an already verified user.</summary>
    /// <param name="user">The verified, active user.</param>
    /// <param name="membership">The active tenant membership, if any.</param>
    /// <param name="userAgent">The client user agent.</param>
    /// <param name="ipAddress">The client IP address.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    public async Task<Result<AuthenticationTokensResponse>> IssueAsync(
        User user,
        TenantMembership? membership,
        string? userAgent,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.Status != UserStatus.Active)
        {
            return Result.Failure<AuthenticationTokensResponse>(IdentityApplicationErrors.InvalidCredentials);
        }

        if (membership is not null && membership.Status != MembershipStatus.Active)
        {
            return Result.Failure<AuthenticationTokensResponse>(IdentityApplicationErrors.InvalidTenant);
        }

        DateTimeOffset nowUtc = _timeProvider.GetUtcNow();

        IReadOnlyCollection<string> permissions = await _permissionResolver
            .ResolveAsync(user, membership, cancellationToken)
            .ConfigureAwait(false);

        await EnforceSessionLimitAsync(user.Id, nowUtc, cancellationToken).ConfigureAwait(false);

        string refreshToken = _tokenGenerator.GenerateToken();
        var sessionResult = UserSession.Start(
            SessionId.New(),
            user.Id,
            membership?.TenantId,
            user.SecurityStamp.Value,
            _refreshTokenHasher.Hash(refreshToken),
            SessionMetadata.Create(userAgent, ipAddress),
            nowUtc,
            SessionPolicy.Default);

        if (sessionResult.IsFailure)
        {
            return Result.Failure<AuthenticationTokensResponse>(sessionResult.Error);
        }

        UserSession session = sessionResult.Value;

        AccessToken accessToken = _accessTokenIssuer.Issue(new AccessTokenDescriptor(
            user.Id.Value,
            membership?.TenantId.Value,
            user.Principal.Value,
            UserPrincipalType,
            user.Email.Value,
            user.Name.Value,
            permissions,
            user.SecurityStamp.Value,
            session.Id.Value));

        await _sessions.AddAsync(session, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(new AuthenticationTokensResponse(accessToken.Token, accessToken.ExpiresAtUtc, refreshToken));
    }

    private async Task EnforceSessionLimitAsync(
        UserId userId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<UserSession> sessions = await _sessions
            .ListActiveByUserAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        List<UserSession> active = sessions.Where(session => session.IsActive(nowUtc)).ToList();

        if (active.Count < MaxActiveSessions)
        {
            return;
        }

        foreach (UserSession oldest in active
            .OrderBy(session => session.CreatedOnUtc)
            .Take(active.Count - MaxActiveSessions + 1))
        {
            oldest.Revoke(nowUtc, "session_limit");
        }
    }
}
