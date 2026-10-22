using BSX.Modules.Sales.Domain.Errors;
using BSX.SharedKernel.Primitives;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Sales.Domain.ValueObjects;

/// <summary>The title of an opportunity.</summary>
public sealed class OpportunityTitle : ValueObject
{
    /// <summary>Maximum supported length.</summary>
    public const int MaxLength = 200;

    private OpportunityTitle(string value) => Value = value;

    /// <summary>Gets the normalized (trimmed) value.</summary>
    public string Value { get; }

    /// <summary>Creates a validated opportunity title.</summary>
    /// <param name="value">The raw title.</param>
    public static Result<OpportunityTitle> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<OpportunityTitle>(SalesErrors.OpportunityTitleInvalid);
        }

        string normalized = value.Trim();

        if (normalized.Length > MaxLength)
        {
            return Result.Failure<OpportunityTitle>(SalesErrors.OpportunityTitleInvalid);
        }

        return Result.Success(new OpportunityTitle(normalized));
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
