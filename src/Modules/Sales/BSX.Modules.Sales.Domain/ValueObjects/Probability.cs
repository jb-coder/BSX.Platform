using BSX.Modules.Sales.Domain.Errors;
using BSX.SharedKernel.Primitives;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Sales.Domain.ValueObjects;

/// <summary>A probability of winning, expressed as a whole percentage from 0 to 100.</summary>
public sealed class Probability : ValueObject
{
    /// <summary>The minimum percentage.</summary>
    public const int MinPercent = 0;

    /// <summary>The maximum percentage.</summary>
    public const int MaxPercent = 100;

    private Probability(int percent) => Percent = percent;

    /// <summary>Gets a probability of 100%.</summary>
    public static Probability Certain { get; } = new(MaxPercent);

    /// <summary>Gets a probability of 0%.</summary>
    public static Probability None { get; } = new(MinPercent);

    /// <summary>Gets the probability percentage.</summary>
    public int Percent { get; }

    /// <summary>Creates a validated probability.</summary>
    /// <param name="percent">The percentage, from 0 to 100.</param>
    public static Result<Probability> Create(int percent)
        => percent is < MinPercent or > MaxPercent
            ? Result.Failure<Probability>(SalesErrors.ProbabilityInvalid)
            : Result.Success(new Probability(percent));

    /// <summary>Gets the probability as a fraction between 0 and 1.</summary>
    public decimal AsFraction() => Percent / 100m;

    /// <inheritdoc />
    public override string ToString() => $"{Percent}%";

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Percent;
    }
}
