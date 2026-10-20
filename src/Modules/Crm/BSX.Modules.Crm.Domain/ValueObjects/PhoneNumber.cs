using BSX.Modules.Crm.Domain.Errors;
using BSX.SharedKernel.Primitives;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Crm.Domain.ValueObjects;

/// <summary>A phone number value object.</summary>
public sealed class PhoneNumber : ValueObject
{
    /// <summary>Maximum supported length.</summary>
    public const int MaxLength = 32;

    /// <summary>Minimum number of digits required.</summary>
    public const int MinDigits = 5;

    private PhoneNumber(string value) => Value = value;

    /// <summary>Gets the normalized (trimmed) value.</summary>
    public string Value { get; }

    /// <summary>Creates a validated phone number.</summary>
    /// <param name="value">The raw phone number.</param>
    public static Result<PhoneNumber> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<PhoneNumber>(CrmErrors.PhoneNumberInvalid);
        }

        string normalized = value.Trim();

        if (normalized.Length > MaxLength
            || !normalized.All(IsAllowed)
            || normalized.Count(char.IsDigit) < MinDigits)
        {
            return Result.Failure<PhoneNumber>(CrmErrors.PhoneNumberInvalid);
        }

        return Result.Success(new PhoneNumber(normalized));
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    private static bool IsAllowed(char character)
        => char.IsDigit(character) || character is '+' or '-' or ' ' or '(' or ')' or '.';
}
