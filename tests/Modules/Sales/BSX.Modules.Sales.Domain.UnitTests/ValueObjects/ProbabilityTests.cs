using BSX.Modules.Sales.Domain.Errors;
using BSX.Modules.Sales.Domain.ValueObjects;

namespace BSX.Modules.Sales.Domain.UnitTests.ValueObjects;

public sealed class ProbabilityTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void Should_Create_WhenInRange(int percent)
    {
        Probability.Create(percent).Value.Percent.Should().Be(percent);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Should_Fail_WhenOutOfRange(int percent)
    {
        var result = Probability.Create(percent);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.ProbabilityInvalid);
    }

    [Fact]
    public void Should_ExposeCertainAndNone()
    {
        Probability.Certain.Percent.Should().Be(100);
        Probability.None.Percent.Should().Be(0);
    }

    [Fact]
    public void Should_ConvertToFraction()
    {
        Probability.Create(25).Value.AsFraction().Should().Be(0.25m);
        Probability.Create(100).Value.AsFraction().Should().Be(1m);
    }

    [Fact]
    public void Should_BeEqual_ByPercent()
    {
        Probability.Create(25).Value.Should().Be(Probability.Create(25).Value);
        Probability.Create(25).Value.Should().NotBe(Probability.Create(26).Value);
    }
}
