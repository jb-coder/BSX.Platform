using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Application.Dtos;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.Memberships;
using BSX.Modules.Identity.Domain.Sessions;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.Users.Credentials;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Authentication.Login;

/// <summary>Handles <see cref="LoginCommand"/>.</summary>
public sealed class LoginCommandHandler : ICommandHandler<LoginCommand, AuthenticationTokensResponse>
{
    private const int MaxActiveSessions = 10;
    private const string UserPrincipalType = "User";

    private readonly IUserRepository _users;
    private readonly IUserSessionRepository _sessions;
    private readonly ITenantMembershipRepository _memberships;
    private readonly PermissionResolver _permissionResolver;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISecureTokenGenerator _tokenGenerator;
    private readonly IRefreshTokenHasher _refreshTokenHasher;
    private readonly IAccessTokenIssuer _accessTokenIssuer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="LoginCommandHandler"/> class.</summary>
    public LoginCommandHandler(
        IUserRepository users,
        IUserSessionRepository sessions,
        ITenantMembershipRepository memberships,
        PermissionResolver permissionResolver,
        IPasswordHasher passwordHasher,
        ISecureTokenGenerator tokenGenerator,
        IRefreshTokenHasher refreshTokenHasher,
        IAccessTokenIssuer accessTokenIssuer,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _users = users;
        _sessions = sessions;
        _memberships = memberships;
        _permissionResolver = permissionResolver;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _refreshTokenHasher = refreshTokenHasher;
        _accessTokenIssuer = accessTokenIssuer;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<Result<AuthenticationTokensResponse>> HandleAsync(LoginCommand request, CancellationToken cancellationToken = default)
    {
        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
        {
            return Result.Failure<AuthenticationTokensResponse>(IdentityApplicationErrors.InvalidCredentials);
        }

        User? user = await _users.GetByEmailAsync(emailResult.Value, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return Result.Failure<AuthenticationTokensResponse>(IdentityApplicationErrors.InvalidCredentials);
        }

        DateTimeOffset nowUtc = _timeProvider.GetUtcNow();

        if (user.Status == UserStatus.Locked && user.LockoutEndUtc is { } lockoutEnd && nowUtc >= lockoutEnd)
        {
            user.Unlock();
        }

        if (user.Status is UserStatus.Disabled or UserStatus.Pending or UserStatus.Locked)
        {
            return Result.Failure<AuthenticationTokensResponse>(IdentityApplicationErrors.UserNotActive);
        }

        PasswordCredential? credential = user.Credentials.OfType<PasswordCredential>().FirstOrDefault();
        if (credential is null || !_passwordHasher.Verify(request.Password, credential.Hash))
        {
            user.RecordAccessFailure(nowUtc);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Failure<AuthenticationTokensResponse>(IdentityApplicationErrors.InvalidCredentials);
        }

        Result<TenantMembership?> membershipResult = await ResolveMembershipAsync(user, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (membershipResult.IsFailure)
        {
            return Result.Failure<AuthenticationTokensResponse>(membershipResult.Error);
        }

        TenantMembership? membership = membershipResult.Value;
        user.ResetAccessFailures();

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
            SessionMetadata.Create(request.UserAgent, request.IpAddress),
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
            permissions,
            user.SecurityStamp.Value,
            session.Id.Value));

        await _sessions.AddAsync(session, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(new AuthenticationTokensResponse(accessToken.Token, accessToken.ExpiresAtUtc, refreshToken));
    }

    private async Task<Result<TenantMembership?>> ResolveMembershipAsync(User user, Guid? tenantId, CancellationToken cancellationToken)
    {
        if (tenantId is { } requestedTenant)
        {
            if (requestedTenant == Guid.Empty)
            {
                return Result.Failure<TenantMembership?>(IdentityApplicationErrors.InvalidTenant);
            }

            TenantMembership? membership = await _memberships
                .FindAsync(TenantId.From(requestedTenant), user.Id, cancellationToken)
                .ConfigureAwait(false);

            if (membership is null || membership.Status != MembershipStatus.Active)
            {
                return Result.Failure<TenantMembership?>(IdentityApplicationErrors.InvalidTenant);
            }

            return Result.Success<TenantMembership?>(membership);
        }

        IReadOnlyCollection<TenantMembership> memberships = await _memberships
            .ListByUserAsync(user.Id, cancellationToken)
            .ConfigureAwait(false);

        List<TenantMembership> active = memberships.Where(membership => membership.Status == MembershipStatus.Active).ToList();

        if (active.Count == 0)
        {
            return Result.Success<TenantMembership?>(null);
        }

        if (active.Count == 1)
        {
            return Result.Success<TenantMembership?>((TenantMembership?)active[0]);
        }

        return Result.Failure<TenantMembership?>(IdentityApplicationErrors.InvalidTenant);
    }

    private async Task EnforceSessionLimitAsync(UserId userId, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<UserSession> sessions = await _sessions
            .ListActiveByUserAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        List<UserSession> active = sessions.Where(session => session.IsActive(nowUtc)).ToList();

        if (active.Count < MaxActiveSessions)
        {
            return;
        }

        foreach (UserSession oldest in active.OrderBy(session => session.CreatedOnUtc).Take(active.Count - MaxActiveSessions + 1))
        {
            oldest.Revoke(nowUtc, "session_limit");
        }
    }
}
