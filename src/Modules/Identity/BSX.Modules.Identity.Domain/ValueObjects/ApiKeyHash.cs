using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.ValueObjects;

/// <summary>A hashed API key secret. The plaintext secret is shown once and never stored.</summary>
public sealed class ApiKeyHash : ValueObject
{
    private ApiKeyHash(string value) => Value = value;

    /// <summary>Gets the hash value.</summary>
    public string Value { get; }

    /// <summary>Creates a key hash from an existing value.</summary>
    /// <param name="value">The hash value.</param>
    public static ApiKeyHash From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("The API key hash cannot be empty.", nameof(value));
        }

        return new ApiKeyHash(value);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
