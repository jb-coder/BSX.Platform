using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.ValueObjects;

/// <summary>A non-secret, displayable API key prefix.</summary>
public sealed class ApiKeyPrefix : ValueObject
{
    private ApiKeyPrefix(string value) => Value = value;

    /// <summary>Gets the prefix value.</summary>
    public string Value { get; }

    /// <summary>Creates a prefix from an existing value.</summary>
    /// <param name="value">The prefix value.</param>
    public static ApiKeyPrefix From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("The API key prefix cannot be empty.", nameof(value));
        }

        return new ApiKeyPrefix(value);
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
