using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Application.Dtos;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.Memberships;
using BSX.Modules.Identity.Domain.Sessions;
using BSX.Modules.Identity.Domain.Users;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Authentication.RefreshToken;

/// <summary>Handles <see cref="RefreshTokenCommand"/>.</summary>
public sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, AuthenticationTokensResponse>
{
    private const string UserPrincipalType = "User";

    private readonly IUserRepository _users;
    private readonly IUserSessionRepository _sessions;
    private readonly ITenantMembershipRepository _memberships;
    private readonly PermissionResolver _permissionResolver;
    private readonly IRefreshTokenHasher _refreshTokenHasher;
    private readonly ISecureTokenGenerator _tokenGenerator;
    private readonly IAccessTokenIssuer _accessTokenIssuer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="RefreshTokenCommandHandler"/> class.</summary>
    public RefreshTokenCommandHandler(
        IUserRepository users,
        IUserSessionRepository sessions,
        ITenantMembershipRepository memberships,
        PermissionResolver permissionResolver,
        IRefreshTokenHasher refreshTokenHasher,
        ISecureTokenGenerator tokenGenerator,
        IAccessTokenIssuer accessTokenIssuer,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _users = users;
        _sessions = sessions;
        _memberships = memberships;
        _permissionResolver = permissionResolver;
        _refreshTokenHasher = refreshTokenHasher;
        _tokenGenerator = tokenGenerator;
        _accessTokenIssuer = accessTokenIssuer;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<Result<AuthenticationTokensResponse>> HandleAsync(RefreshTokenCommand request, CancellationToken cancellationToken = default)
    {
        DateTimeOffset nowUtc = _timeProvider.GetUtcNow();
        var providedHash = _refreshTokenHasher.Hash(request.RefreshToken);

        UserSession? session = await _sessions.FindByRefreshTokenAsync(providedHash, cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            return Result.Failure<AuthenticationTokensResponse>(IdentityApplicationErrors.InvalidRefreshToken);
        }

        User? user = await _users.GetByIdAsync(session.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return Result.Failure<AuthenticationTokensResponse>(IdentityApplicationErrors.InvalidRefreshToken);
        }

        // Security stamp is validated here, at refresh, not on every request (ADR-017).
        if (!string.Equals(session.SecurityStamp, user.SecurityStamp.Value, StringComparison.Ordinal))
        {
            session.Revoke(nowUtc, "security_stamp_changed");
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Failure<AuthenticationTokensResponse>(IdentityApplicationErrors.InvalidRefreshToken);
        }

        string newRefreshToken = _tokenGenerator.GenerateToken();
        Result rotation = session.Rotate(providedHash, _refreshTokenHasher.Hash(newRefreshToken), nowUtc, SessionPolicy.Default);

        if (rotation.IsFailure)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Failure<AuthenticationTokensResponse>(rotation.Error);
        }

        TenantMembership? membership = session.TenantId is { } tenantId
            ? await _memberships.FindAsync(tenantId, user.Id, cancellationToken).ConfigureAwait(false)
            : null;

        if (membership is not null && membership.Status != MembershipStatus.Active)
        {
            membership = null;
        }

        IReadOnlyCollection<string> permissions = await _permissionResolver
            .ResolveAsync(user, membership, cancellationToken)
            .ConfigureAwait(false);

        AccessToken accessToken = _accessTokenIssuer.Issue(new AccessTokenDescriptor(
            user.Id.Value,
            session.TenantId?.Value,
            user.Principal.Value,
            UserPrincipalType,
            permissions,
            user.SecurityStamp.Value,
            session.Id.Value));

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(new AuthenticationTokensResponse(accessToken.Token, accessToken.ExpiresAtUtc, newRefreshToken));
    }
}
