using System.Globalization;
using BSX.Modules.Sales.Domain.Errors;
using BSX.SharedKernel.Primitives;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Sales.Domain.ValueObjects;

/// <summary>
/// A monetary amount in a single ISO 4217 currency. Amounts are non-negative and limited to two
/// decimal places, matching commercial revenue figures.
/// </summary>
public sealed class Money : ValueObject
{
    /// <summary>Maximum supported amount.</summary>
    public const decimal MaxAmount = 1_000_000_000_000m;

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>Gets the amount.</summary>
    public decimal Amount { get; }

    /// <summary>Gets the normalized (upper-case) ISO 4217 currency code.</summary>
    public string Currency { get; }

    /// <summary>Creates a validated monetary amount.</summary>
    /// <param name="amount">The amount; non-negative, at most two decimal places.</param>
    /// <param name="currency">The ISO 4217 currency code.</param>
    public static Result<Money> Create(decimal amount, string? currency)
    {
        if (amount < 0
            || amount > MaxAmount
            || decimal.Round(amount, 2) != amount)
        {
            return Result.Failure<Money>(SalesErrors.MoneyAmountInvalid);
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            return Result.Failure<Money>(SalesErrors.MoneyCurrencyInvalid);
        }

        string normalized = currency.Trim().ToUpperInvariant();

        if (normalized.Length != 3 || !normalized.All(character => character is >= 'A' and <= 'Z'))
        {
            return Result.Failure<Money>(SalesErrors.MoneyCurrencyInvalid);
        }

        return Result.Success(new Money(amount, normalized));
    }

    /// <summary>
    /// Applies a percentage to the amount (revenue-weighting). The result is rounded to two decimal
    /// places using <see cref="MidpointRounding.AwayFromZero"/>.
    /// </summary>
    /// <param name="percent">The percentage, from 0 to 100.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the percentage is out of range.</exception>
    public Money ApplyPercentage(int percent)
    {
        if (percent is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percent), percent, "The percentage must be between 0 and 100.");
        }

        decimal weighted = decimal.Round(Amount * percent / 100m, 2, MidpointRounding.AwayFromZero);
        return new Money(weighted, Currency);
    }

    /// <inheritdoc />
    public override string ToString()
        => $"{Amount.ToString("0.00", CultureInfo.InvariantCulture)} {Currency}";

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
