namespace BSX.Modules.Identity.Domain.ValueObjects;

/// <summary>Strongly typed identifier for a password reset token.</summary>
public sealed class PasswordResetTokenId : IdentityId
{
    private PasswordResetTokenId(Guid value)
        : base(value)
    {
    }

    /// <summary>Creates a new identifier.</summary>
    public static PasswordResetTokenId New() => new(Guid.NewGuid());

    /// <summary>Creates an identifier from an existing value.</summary>
    public static PasswordResetTokenId From(Guid value) => new(value);
}
