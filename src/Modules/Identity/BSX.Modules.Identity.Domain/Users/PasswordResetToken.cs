using BSX.Modules.Identity.Domain.Errors;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Domain.Users;

/// <summary>
/// A single-use, time-limited password reset token owned by the <see cref="User"/> aggregate.
/// Only the hash is stored (ADR-016).
/// </summary>
public sealed class PasswordResetToken : Entity<PasswordResetTokenId>
{
    internal PasswordResetToken(
        PasswordResetTokenId id,
        PasswordResetTokenHash hash,
        DateTimeOffset issuedOnUtc,
        DateTimeOffset expiresOnUtc)
        : base(id)
    {
        Hash = hash;
        IssuedOnUtc = issuedOnUtc;
        ExpiresOnUtc = expiresOnUtc;
    }

    private PasswordResetToken()
    {
    }

    /// <summary>Gets the stored token hash.</summary>
    public PasswordResetTokenHash Hash { get; private set; } = null!;

    /// <summary>Gets the issuance timestamp.</summary>
    public DateTimeOffset IssuedOnUtc { get; private set; }

    /// <summary>Gets the expiry timestamp.</summary>
    public DateTimeOffset ExpiresOnUtc { get; private set; }

    /// <summary>Gets the timestamp the token was used, if any.</summary>
    public DateTimeOffset? UsedOnUtc { get; private set; }

    /// <summary>Gets a value indicating whether the token has been used.</summary>
    public bool IsUsed => UsedOnUtc.HasValue;

    /// <summary>Determines whether the token has expired at the given time.</summary>
    /// <param name="nowUtc">The current UTC time.</param>
    public bool IsExpiredAt(DateTimeOffset nowUtc) => nowUtc >= ExpiresOnUtc;

    /// <summary>Validates a provided token hash against this token.</summary>
    /// <param name="providedHash">The presented token hash.</param>
    /// <param name="nowUtc">The current UTC time.</param>
    public Result Validate(PasswordResetTokenHash providedHash, DateTimeOffset nowUtc)
    {
        if (Hash != providedHash)
        {
            return Result.Failure(IdentityErrors.PasswordResetTokenInvalid);
        }

        if (IsUsed)
        {
            return Result.Failure(IdentityErrors.PasswordResetTokenUsed);
        }

        if (IsExpiredAt(nowUtc))
        {
            return Result.Failure(IdentityErrors.PasswordResetTokenExpired);
        }

        return Result.Success();
    }

    /// <summary>Marks the token as used.</summary>
    /// <param name="nowUtc">The current UTC time.</param>
    public void MarkUsed(DateTimeOffset nowUtc) => UsedOnUtc = nowUtc;
}
