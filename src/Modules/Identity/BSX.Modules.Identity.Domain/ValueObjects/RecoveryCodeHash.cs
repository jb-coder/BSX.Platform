using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.ValueObjects;

/// <summary>A hashed one-time recovery code.</summary>
public sealed class RecoveryCodeHash : ValueObject
{
    private RecoveryCodeHash(string value) => Value = value;

    /// <summary>Gets the hash value.</summary>
    public string Value { get; }

    /// <summary>Creates a recovery code hash from an existing value.</summary>
    /// <param name="value">The hash value.</param>
    public static RecoveryCodeHash From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("The recovery code hash cannot be empty.", nameof(value));
        }

        return new RecoveryCodeHash(value);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
