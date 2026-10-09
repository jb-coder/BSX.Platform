using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.ValueObjects;

/// <summary>
/// Base class for strongly typed identity value objects.
/// Two identifiers are equal only when their types and values match.
/// </summary>
public abstract class IdentityId : ValueObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityId"/> class.
    /// </summary>
    /// <param name="value">The underlying identifier value.</param>
    /// <exception cref="ArgumentException">Thrown when the value is empty.</exception>
    protected IdentityId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("An identity value cannot be empty.", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Gets the underlying identifier value.
    /// </summary>
    public Guid Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value.ToString();

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
