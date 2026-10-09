using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.ValueObjects;

/// <summary>
/// An opaque security stamp rotated on security-relevant changes and validated at refresh
/// (ADR-017).
/// </summary>
public sealed class SecurityStamp : ValueObject
{
    private SecurityStamp(string value) => Value = value;

    /// <summary>Gets the opaque value.</summary>
    public string Value { get; }

    /// <summary>Creates a new random security stamp.</summary>
    public static SecurityStamp New() => new(Guid.NewGuid().ToString("N"));

    /// <summary>Creates a security stamp from an existing value.</summary>
    /// <param name="value">The opaque value.</param>
    public static SecurityStamp From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("The security stamp cannot be empty.", nameof(value));
        }

        return new SecurityStamp(value);
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
