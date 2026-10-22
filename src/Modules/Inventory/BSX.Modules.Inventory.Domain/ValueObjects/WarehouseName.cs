using BSX.Modules.Inventory.Domain.Errors;
using BSX.SharedKernel.Primitives;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Inventory.Domain.ValueObjects;

/// <summary>The display name of a warehouse.</summary>
public sealed class WarehouseName : ValueObject
{
    /// <summary>Maximum supported length.</summary>
    public const int MaxLength = 200;

    private WarehouseName(string value) => Value = value;

    /// <summary>Gets the normalized (trimmed) value.</summary>
    public string Value { get; }

    /// <summary>Creates a validated warehouse name.</summary>
    /// <param name="value">The raw name value.</param>
    public static Result<WarehouseName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<WarehouseName>(InventoryErrors.WarehouseNameInvalid);
        }

        string normalized = value.Trim();

        if (normalized.Length > MaxLength)
        {
            return Result.Failure<WarehouseName>(InventoryErrors.WarehouseNameInvalid);
        }

        return Result.Success(new WarehouseName(normalized));
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
