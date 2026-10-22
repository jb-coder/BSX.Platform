using System.Globalization;
using BSX.Modules.Sales.Domain.Errors;
using BSX.SharedKernel.Primitives;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Sales.Domain.ValueObjects;

/// <summary>
/// The expected date on which an opportunity will close. The date may be today or in the future and
/// is bounded to a practical forecasting horizon.
/// </summary>
public sealed class ExpectedCloseDate : ValueObject
{
    /// <summary>Maximum forecasting horizon, in years.</summary>
    public const int MaxHorizonYears = 10;

    private ExpectedCloseDate(DateOnly value) => Value = value;

    /// <summary>Gets the expected close date.</summary>
    public DateOnly Value { get; }

    /// <summary>Creates a validated expected close date.</summary>
    /// <param name="value">The expected close date.</param>
    /// <param name="today">The current date, used to reject dates in the past.</param>
    public static Result<ExpectedCloseDate> Create(DateOnly value, DateOnly today)
    {
        if (value == default
            || value < today
            || value > today.AddYears(MaxHorizonYears))
        {
            return Result.Failure<ExpectedCloseDate>(SalesErrors.ExpectedCloseDateInvalid);
        }

        return Result.Success(new ExpectedCloseDate(value));
    }

    /// <summary>Determines whether the expected close date has passed.</summary>
    /// <param name="today">The current date.</param>
    public bool IsPast(DateOnly today) => Value < today;

    /// <inheritdoc />
    public override string ToString() => Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
