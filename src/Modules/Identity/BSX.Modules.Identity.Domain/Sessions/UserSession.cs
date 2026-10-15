using BSX.Modules.Identity.Domain.DomainEvents;
using BSX.Modules.Identity.Domain.Errors;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Domain.Sessions;

/// <summary>
/// A refresh-token session owning a linear rotation chain with reuse detection and a grace window
/// (ADR-017). Sessions are principal-scoped; the active tenant is captured on the session.
/// </summary>
public sealed class UserSession : AggregateRoot<SessionId>
{
    private readonly List<RefreshToken> _tokens = [];

    private UserSession(
        SessionId id,
        UserId userId,
        TenantId? tenantId,
        RefreshTokenHash hash,
        SessionMetadata metadata,
        DateTimeOffset nowUtc,
        SessionPolicy policy)
        : base(id)
    {
        UserId = userId;
        TenantId = tenantId;
        Metadata = metadata;
        Status = SessionStatus.Active;
        CreatedOnUtc = nowUtc;
        LastRefreshedOnUtc = nowUtc;
        IdleExpiresOnUtc = nowUtc.Add(policy.IdleTimeout);
        AbsoluteExpiresOnUtc = nowUtc.Add(policy.AbsoluteLifetime);
        _tokens.Add(new RefreshToken(RefreshTokenId.New(), hash, nowUtc, nowUtc.Add(policy.TokenLifetime)));
    }

    private UserSession()
    {
    }

    /// <summary>Gets the owning user identifier.</summary>
    public UserId UserId { get; private set; } = null!;

    /// <summary>Gets the tenant active at session start, if any.</summary>
    public TenantId? TenantId { get; private set; }

    /// <summary>Gets the session status.</summary>
    public SessionStatus Status { get; private set; }

    /// <summary>Gets the client metadata.</summary>
    public SessionMetadata Metadata { get; private set; } = null!;

    /// <summary>Gets the creation timestamp.</summary>
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <summary>Gets the last refresh timestamp.</summary>
    public DateTimeOffset LastRefreshedOnUtc { get; private set; }

    /// <summary>Gets the idle-expiry timestamp.</summary>
    public DateTimeOffset IdleExpiresOnUtc { get; private set; }

    /// <summary>Gets the absolute-expiry timestamp.</summary>
    public DateTimeOffset AbsoluteExpiresOnUtc { get; private set; }

    /// <summary>Gets the revocation timestamp, if any.</summary>
    public DateTimeOffset? RevokedOnUtc { get; private set; }

    /// <summary>Gets the revocation reason, if any.</summary>
    public string? RevokedReason { get; private set; }

    /// <summary>Gets the rotation chain.</summary>
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _tokens.AsReadOnly();

    /// <summary>Gets the currently active (usable) token, if any.</summary>
    public RefreshToken? CurrentToken => _tokens.FindLast(token => !token.IsUsed && token.RevokedOnUtc is null);

    /// <summary>Starts a new session with the first refresh token.</summary>
    public static Result<UserSession> Start(
        SessionId id,
        UserId userId,
        TenantId? tenantId,
        RefreshTokenHash hash,
        SessionMetadata metadata,
        DateTimeOffset nowUtc,
        SessionPolicy policy)
    {
        var session = new UserSession(id, userId, tenantId, hash, metadata, nowUtc, policy);
        session.RaiseDomainEvent(new SessionStarted(id, userId, tenantId?.Value));
        return Result.Success(session);
    }

    /// <summary>Determines whether the session is usable at the given time.</summary>
    /// <param name="nowUtc">The current UTC time.</param>
    public bool IsActive(DateTimeOffset nowUtc)
        => Status == SessionStatus.Active
            && RevokedOnUtc is null
            && nowUtc < AbsoluteExpiresOnUtc
            && nowUtc < IdleExpiresOnUtc;

    /// <summary>Rotates the refresh chain using the presented token hash.</summary>
    /// <param name="providedHash">The presented token hash.</param>
    /// <param name="newHash">The hash of the newly issued token.</param>
    /// <param name="nowUtc">The current UTC time.</param>
    /// <param name="policy">The session policy.</param>
    public Result Rotate(RefreshTokenHash providedHash, RefreshTokenHash newHash, DateTimeOffset nowUtc, SessionPolicy policy)
    {
        if (!IsActive(nowUtc))
        {
            return Result.Failure(IdentityErrors.SessionExpired);
        }

        RefreshToken? token = _tokens.Find(candidate => candidate.Hash == providedHash);

        if (token is null || token.RevokedOnUtc is not null)
        {
            return Result.Failure(IdentityErrors.RefreshTokenInvalid);
        }

        if (token.UsedOnUtc is { } usedOnUtc)
        {
            if (nowUtc - usedOnUtc <= policy.GraceWindow)
            {
                // Legitimate concurrent refresh: rotate again from the just-used token.
                return RotateCore(token, newHash, nowUtc, policy);
            }

            RevokeCore(nowUtc, "refresh_token_reuse");
            RaiseDomainEvent(new RefreshTokenReuseDetected(Id, UserId, token.Id));
            RaiseDomainEvent(new SessionRevoked(Id, UserId, "refresh_token_reuse"));
            return Result.Failure(IdentityErrors.RefreshTokenReuseDetected);
        }

        if (token.IsExpiredAt(nowUtc))
        {
            return Result.Failure(IdentityErrors.RefreshTokenExpired);
        }

        return RotateCore(token, newHash, nowUtc, policy);
    }

    /// <summary>Revokes the session.</summary>
    /// <param name="nowUtc">The current UTC time.</param>
    /// <param name="reason">The revocation reason.</param>
    public Result Revoke(DateTimeOffset nowUtc, string reason)
    {
        if (Status == SessionStatus.Revoked)
        {
            return Result.Success();
        }

        RevokeCore(nowUtc, reason);
        RaiseDomainEvent(new SessionRevoked(Id, UserId, reason));
        return Result.Success();
    }

    private Result RotateCore(RefreshToken previous, RefreshTokenHash newHash, DateTimeOffset nowUtc, SessionPolicy policy)
    {
        if (previous.ReplacedByTokenId is { } replacedById)
        {
            _tokens.Find(token => token.Id == replacedById)?.Revoke(nowUtc);
        }

        var newToken = new RefreshToken(RefreshTokenId.New(), newHash, nowUtc, nowUtc.Add(policy.TokenLifetime));
        previous.MarkUsed(nowUtc);
        previous.SetReplacedBy(newToken.Id);
        _tokens.Add(newToken);

        LastRefreshedOnUtc = nowUtc;
        IdleExpiresOnUtc = nowUtc.Add(policy.IdleTimeout);
        RaiseDomainEvent(new RefreshTokenRotated(Id, UserId, newToken.Id));
        return Result.Success();
    }

    private void RevokeCore(DateTimeOffset nowUtc, string reason)
    {
        Status = SessionStatus.Revoked;
        RevokedOnUtc = nowUtc;
        RevokedReason = reason;
    }
}
