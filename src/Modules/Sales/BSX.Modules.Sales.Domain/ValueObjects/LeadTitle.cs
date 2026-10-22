using BSX.Modules.Sales.Domain.Errors;
using BSX.SharedKernel.Primitives;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Sales.Domain.ValueObjects;

/// <summary>The title of a lead.</summary>
public sealed class LeadTitle : ValueObject
{
    /// <summary>Maximum supported length.</summary>
    public const int MaxLength = 200;

    private LeadTitle(string value) => Value = value;

    /// <summary>Gets the normalized (trimmed) value.</summary>
    public string Value { get; }

    /// <summary>Creates a validated lead title.</summary>
    /// <param name="value">The raw title.</param>
    public static Result<LeadTitle> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<LeadTitle>(SalesErrors.LeadTitleInvalid);
        }

        string normalized = value.Trim();

        if (normalized.Length > MaxLength)
        {
            return Result.Failure<LeadTitle>(SalesErrors.LeadTitleInvalid);
        }

        return Result.Success(new LeadTitle(normalized));
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
