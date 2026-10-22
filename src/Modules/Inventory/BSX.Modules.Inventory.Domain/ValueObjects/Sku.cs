using BSX.Modules.Inventory.Domain.Errors;
using BSX.SharedKernel.Primitives;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Inventory.Domain.ValueObjects;

/// <summary>
/// A stock-keeping unit code, unique per tenant. Uniqueness is an Infrastructure concern; the
/// value object guarantees normalization and a safe character set.
/// </summary>
public sealed class Sku : ValueObject
{
    /// <summary>Maximum supported length.</summary>
    public const int MaxLength = 64;

    private Sku(string value) => Value = value;

    /// <summary>Gets the normalized (trimmed, upper-case invariant) value.</summary>
    public string Value { get; }

    /// <summary>Creates a validated SKU.</summary>
    /// <param name="value">The raw SKU value.</param>
    public static Result<Sku> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<Sku>(InventoryErrors.SkuInvalid);
        }

        string normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length > MaxLength || !normalized.All(IsAllowed))
        {
            return Result.Failure<Sku>(InventoryErrors.SkuInvalid);
        }

        return Result.Success(new Sku(normalized));
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    private static bool IsAllowed(char character)
        => char.IsLetterOrDigit(character) || character is '-' or '_' or '.';
}
