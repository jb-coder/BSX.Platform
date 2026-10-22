using BSX.Modules.Sales.Domain.Errors;
using BSX.Modules.Sales.Domain.ValueObjects;

namespace BSX.Modules.Sales.Domain.UnitTests.ValueObjects;

public sealed class ExpectedCloseDateTests
{
    private static readonly DateOnly Today = new(2026, 1, 1);

    [Fact]
    public void Should_AcceptToday()
    {
        ExpectedCloseDate.Create(Today, Today).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Should_AcceptFutureDate()
    {
        ExpectedCloseDate.Create(Today.AddDays(30), Today).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Should_AcceptTheMaximumHorizon()
    {
        ExpectedCloseDate.Create(Today.AddYears(ExpectedCloseDate.MaxHorizonYears), Today)
            .IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenDateIsInThePast()
    {
        var result = ExpectedCloseDate.Create(Today.AddDays(-1), Today);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.ExpectedCloseDateInvalid);
    }

    [Fact]
    public void Should_Fail_WhenDateIsBeyondTheHorizon()
    {
        var result = ExpectedCloseDate.Create(
            Today.AddYears(ExpectedCloseDate.MaxHorizonYears).AddDays(1),
            Today);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.ExpectedCloseDateInvalid);
    }

    [Fact]
    public void Should_Fail_WhenDateIsDefault()
    {
        var result = ExpectedCloseDate.Create(default, Today);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.ExpectedCloseDateInvalid);
    }

    [Fact]
    public void Should_ReportPastDates()
    {
        ExpectedCloseDate date = ExpectedCloseDate.Create(Today, Today).Value;

        date.IsPast(Today).Should().BeFalse();
        date.IsPast(Today.AddDays(1)).Should().BeTrue();
    }

    [Fact]
    public void Should_FormatAsIsoDate()
    {
        ExpectedCloseDate.Create(Today, Today).Value.ToString().Should().Be("2026-01-01");
    }

    [Fact]
    public void Should_BeEqual_ByDate()
    {
        ExpectedCloseDate.Create(Today, Today).Value.Should()
            .Be(ExpectedCloseDate.Create(Today, Today).Value);
    }
}
