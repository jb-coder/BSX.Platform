using BSX.Modules.Crm.Domain.Errors;
using BSX.SharedKernel.Primitives;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Crm.Domain.ValueObjects;

/// <summary>A party's tax identifier (for example a VAT or national tax number).</summary>
public sealed class TaxIdentifier : ValueObject
{
    /// <summary>Maximum supported length.</summary>
    public const int MaxLength = 50;

    private TaxIdentifier(string value) => Value = value;

    /// <summary>Gets the normalized (trimmed, upper-case invariant) value.</summary>
    public string Value { get; }

    /// <summary>Creates a validated tax identifier.</summary>
    /// <param name="value">The raw tax identifier.</param>
    public static Result<TaxIdentifier> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<TaxIdentifier>(CrmErrors.TaxIdentifierInvalid);
        }

        string normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length > MaxLength || !normalized.All(IsAllowed))
        {
            return Result.Failure<TaxIdentifier>(CrmErrors.TaxIdentifierInvalid);
        }

        return Result.Success(new TaxIdentifier(normalized));
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    private static bool IsAllowed(char character)
        => char.IsLetterOrDigit(character) || character is '-' or '.' or '/';
}
