using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.ValueObjects;

/// <summary>A hashed refresh token. The plaintext token is never stored (ADR-017).</summary>
public sealed class RefreshTokenHash : ValueObject
{
    private RefreshTokenHash(string value) => Value = value;

    /// <summary>Gets the hash value.</summary>
    public string Value { get; }

    /// <summary>Creates a refresh token hash from an existing value.</summary>
    /// <param name="value">The hash value.</param>
    public static RefreshTokenHash From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("The refresh token hash cannot be empty.", nameof(value));
        }

        return new RefreshTokenHash(value);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
