using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.ValueObjects;

/// <summary>
/// A hashed password reset token. The plaintext token is never stored (ADR-016).
/// </summary>
public sealed class PasswordResetTokenHash : ValueObject
{
    private PasswordResetTokenHash(string value) => Value = value;

    /// <summary>Gets the hash value.</summary>
    public string Value { get; }

    /// <summary>Creates a token hash from an existing value.</summary>
    /// <param name="value">The hash value.</param>
    public static PasswordResetTokenHash From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("The token hash cannot be empty.", nameof(value));
        }

        return new PasswordResetTokenHash(value);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
