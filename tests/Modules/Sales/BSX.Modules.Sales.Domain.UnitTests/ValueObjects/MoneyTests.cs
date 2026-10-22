using BSX.Modules.Sales.Domain.Errors;
using BSX.Modules.Sales.Domain.ValueObjects;

namespace BSX.Modules.Sales.Domain.UnitTests.ValueObjects;

public sealed class MoneyTests
{
    [Fact]
    public void Should_Create_AndNormalizeCurrency()
    {
        var result = Money.Create(1234.56m, " usd ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(1234.56m);
        result.Value.Currency.Should().Be("USD");
    }

    [Fact]
    public void Should_AcceptZero()
    {
        Money.Create(0m, "USD").IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenAmountIsNegative()
    {
        var result = Money.Create(-0.01m, "USD");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.MoneyAmountInvalid);
    }

    [Fact]
    public void Should_Fail_WhenAmountHasMoreThanTwoDecimals()
    {
        var result = Money.Create(10.001m, "USD");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.MoneyAmountInvalid);
    }

    [Fact]
    public void Should_Fail_WhenAmountExceedsTheMaximum()
    {
        var result = Money.Create(Money.MaxAmount + 1m, "USD");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.MoneyAmountInvalid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("US")]
    [InlineData("USDX")]
    [InlineData("US1")]
    [InlineData("U$D")]
    public void Should_Fail_WhenCurrencyIsInvalid(string? currency)
    {
        var result = Money.Create(10m, currency);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.MoneyCurrencyInvalid);
    }

    [Fact]
    public void Should_BeEqual_OnlyWhenAmountAndCurrencyMatch()
    {
        Money.Create(10m, "USD").Value.Should().Be(Money.Create(10m, "usd").Value);
        Money.Create(10m, "USD").Value.Should().NotBe(Money.Create(10m, "EUR").Value);
        Money.Create(10m, "USD").Value.Should().NotBe(Money.Create(11m, "USD").Value);
    }

    [Fact]
    public void Should_ApplyPercentage_WithTwoDecimalRounding()
    {
        Money money = Money.Create(10.05m, "USD").Value;

        money.ApplyPercentage(33).Amount.Should().Be(3.32m);
        money.ApplyPercentage(0).Amount.Should().Be(0m);
        money.ApplyPercentage(100).Should().Be(money);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Should_Throw_WhenPercentageIsOutOfRange(int percent)
    {
        Money money = Money.Create(10m, "USD").Value;

        Action act = () => money.ApplyPercentage(percent);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Should_FormatAsAmountAndCurrency()
    {
        Money.Create(1234.5m, "USD").Value.ToString().Should().Be("1234.50 USD");
    }
}
