using BSX.Modules.Inventory.Domain.Errors;
using BSX.SharedKernel.Primitives;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Inventory.Domain.ValueObjects;

/// <summary>The display name of a product.</summary>
public sealed class ProductName : ValueObject
{
    /// <summary>Maximum supported length.</summary>
    public const int MaxLength = 200;

    private ProductName(string value) => Value = value;

    /// <summary>Gets the normalized (trimmed) value.</summary>
    public string Value { get; }

    /// <summary>Creates a validated product name.</summary>
    /// <param name="value">The raw name value.</param>
    public static Result<ProductName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<ProductName>(InventoryErrors.ProductNameInvalid);
        }

        string normalized = value.Trim();

        if (normalized.Length > MaxLength)
        {
            return Result.Failure<ProductName>(InventoryErrors.ProductNameInvalid);
        }

        return Result.Success(new ProductName(normalized));
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
