using BSX.Modules.Crm.Domain.Errors;
using BSX.SharedKernel.Primitives;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Crm.Domain.ValueObjects;

/// <summary>A customer's unique business code within a tenant.</summary>
public sealed class CustomerCode : ValueObject
{
    /// <summary>Maximum supported length.</summary>
    public const int MaxLength = 32;

    private CustomerCode(string value) => Value = value;

    /// <summary>Gets the normalized (trimmed, upper-case invariant) value.</summary>
    public string Value { get; }

    /// <summary>Creates a validated customer code.</summary>
    /// <param name="value">The raw code value.</param>
    public static Result<CustomerCode> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<CustomerCode>(CrmErrors.CustomerCodeInvalid);
        }

        string normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length > MaxLength || !normalized.All(IsAllowed))
        {
            return Result.Failure<CustomerCode>(CrmErrors.CustomerCodeInvalid);
        }

        return Result.Success(new CustomerCode(normalized));
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
