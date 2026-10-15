using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.Sessions;

/// <summary>A single refresh token within a session rotation chain (ADR-017).</summary>
public sealed class RefreshToken : Entity<RefreshTokenId>
{
    internal RefreshToken(
        RefreshTokenId id,
        RefreshTokenHash hash,
        DateTimeOffset issuedOnUtc,
        DateTimeOffset expiresOnUtc)
        : base(id)
    {
        Hash = hash;
        IssuedOnUtc = issuedOnUtc;
        ExpiresOnUtc = expiresOnUtc;
    }

    private RefreshToken()
    {
    }

    /// <summary>Gets the stored token hash.</summary>
    public RefreshTokenHash Hash { get; private set; } = null!;

    /// <summary>Gets the issuance timestamp.</summary>
    public DateTimeOffset IssuedOnUtc { get; private set; }

    /// <summary>Gets the expiry timestamp.</summary>
    public DateTimeOffset ExpiresOnUtc { get; private set; }

    /// <summary>Gets the timestamp the token was rotated/consumed, if any.</summary>
    public DateTimeOffset? UsedOnUtc { get; private set; }

    /// <summary>Gets the revocation timestamp, if any.</summary>
    public DateTimeOffset? RevokedOnUtc { get; private set; }

    /// <summary>Gets the identifier of the token that replaced this one, if any.</summary>
    public RefreshTokenId? ReplacedByTokenId { get; private set; }

    /// <summary>Gets a value indicating whether the token has been used.</summary>
    public bool IsUsed => UsedOnUtc.HasValue;

    /// <summary>Determines whether the token has expired at the given time.</summary>
    /// <param name="nowUtc">The current UTC time.</param>
    public bool IsExpiredAt(DateTimeOffset nowUtc) => nowUtc >= ExpiresOnUtc;

    internal void MarkUsed(DateTimeOffset nowUtc) => UsedOnUtc = nowUtc;

    internal void SetReplacedBy(RefreshTokenId replacedByTokenId) => ReplacedByTokenId = replacedByTokenId;

    internal void Revoke(DateTimeOffset nowUtc) => RevokedOnUtc = nowUtc;
}
