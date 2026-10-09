using BSX.SharedKernel.Primitives;

namespace BSX.SharedKernel.UnitTests.Primitives;

public sealed class ValueObjectTests
{
    [Fact]
    public void Should_BeEqual_WhenAllComponentsMatch()
    {
        var first = new Money(100m, "EUR");
        var second = new Money(100m, "EUR");

        first.Should().Be(second);
        (first == second).Should().BeTrue();
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void Should_NotBeEqual_WhenAnyComponentDiffers()
    {
        var first = new Money(100m, "EUR");
        var second = new Money(100m, "USD");

        first.Should().NotBe(second);
        (first != second).Should().BeTrue();
    }

    private sealed class Money(decimal amount, string currency) : ValueObject
    {
        public decimal Amount { get; } = amount;

        public string Currency { get; } = currency;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }
    }
}
