using BSX.Modules.Crm.Domain.Errors;
using BSX.SharedKernel.Primitives;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Crm.Domain.ValueObjects;

/// <summary>
/// The canonical display name of a party (customer, organization or contact person).
/// </summary>
public sealed class CustomerName : ValueObject
{
    /// <summary>Maximum supported length.</summary>
    public const int MaxLength = 200;

    private CustomerName(string value) => Value = value;

    /// <summary>Gets the normalized (trimmed) value.</summary>
    public string Value { get; }

    /// <summary>Creates a validated name.</summary>
    /// <param name="value">The raw name value.</param>
    public static Result<CustomerName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<CustomerName>(CrmErrors.NameInvalid);
        }

        string normalized = value.Trim();

        if (normalized.Length > MaxLength)
        {
            return Result.Failure<CustomerName>(CrmErrors.NameInvalid);
        }

        return Result.Success(new CustomerName(normalized));
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
