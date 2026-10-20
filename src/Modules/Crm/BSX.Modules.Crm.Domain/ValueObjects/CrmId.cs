using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Crm.Domain.ValueObjects;

/// <summary>
/// Base class for strongly typed CRM identifiers.
/// Two identifiers are equal only when their types and values match.
/// </summary>
public abstract class CrmId : ValueObject
{
    /// <summary>Initializes a new instance of the <see cref="CrmId"/> class.</summary>
    /// <param name="value">The underlying identifier value.</param>
    /// <exception cref="ArgumentException">Thrown when the value is empty.</exception>
    protected CrmId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("A CRM identifier cannot be empty.", nameof(value));
        }

        Value = value;
    }

    /// <summary>Gets the underlying identifier value.</summary>
    public Guid Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value.ToString();

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
