using System.Globalization;
using BSX.Modules.Inventory.Domain.Errors;
using BSX.SharedKernel.Primitives;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Inventory.Domain.ValueObjects;

/// <summary>
/// A non-negative whole-unit stock quantity. Zero is valid (an empty stock position); negative
/// quantities are rejected. Inventory v1 supports discrete units only (ADR-019); fractional
/// quantities and units of measure are deferred to a future ADR.
/// </summary>
public sealed class Quantity : ValueObject
{
    /// <summary>The minimum supported quantity.</summary>
    public const int MinValue = 0;

    /// <summary>The maximum supported quantity.</summary>
    public const int MaxValue = 1_000_000_000;

    private Quantity(int value) => Value = value;

    /// <summary>Gets an empty quantity.</summary>
    public static Quantity Zero { get; } = new(MinValue);

    /// <summary>Gets the quantity value.</summary>
    public int Value { get; }

    /// <summary>Gets a value indicating whether the quantity is zero.</summary>
    public bool IsZero => Value == MinValue;

    /// <summary>Creates a validated quantity.</summary>
    /// <param name="value">The quantity, from 0 to <see cref="MaxValue"/>.</param>
    public static Result<Quantity> Create(int value)
        => value is < MinValue or > MaxValue
            ? Result.Failure<Quantity>(InventoryErrors.QuantityInvalid)
            : Result.Success(new Quantity(value));

    /// <summary>
    /// Creates a quantity from an already-validated value. Internal because it is only used for
    /// derived arithmetic (for example available stock) whose range has already been checked;
    /// callers use <see cref="Create"/> so invalid input remains a <c>Result</c> (ADR-011).
    /// </summary>
    /// <param name="value">The already-validated quantity value.</param>
    internal static Quantity From(int value) => Create(value).Value;

    /// <inheritdoc />
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
