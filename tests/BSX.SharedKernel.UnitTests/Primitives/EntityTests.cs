using BSX.SharedKernel.Primitives;

namespace BSX.SharedKernel.UnitTests.Primitives;

public sealed class EntityTests
{
    [Fact]
    public void Should_BeEqual_WhenIdsAndTypesMatch()
    {
        var id = Guid.NewGuid();

        var first = new TestEntity(id);
        var second = new TestEntity(id);

        first.Should().Be(second);
        (first == second).Should().BeTrue();
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void Should_NotBeEqual_WhenIdsDiffer()
    {
        var first = new TestEntity(Guid.NewGuid());
        var second = new TestEntity(Guid.NewGuid());

        first.Should().NotBe(second);
        (first != second).Should().BeTrue();
    }

    [Fact]
    public void Should_NotBeEqual_WhenTypesDiffer()
    {
        var id = Guid.NewGuid();

        var entity = new TestEntity(id);
        var derived = new DerivedTestEntity(id);

        (entity == derived).Should().BeFalse();
    }

    private sealed class TestEntity(Guid id) : Entity<Guid>(id);

    private sealed class DerivedTestEntity(Guid id) : Entity<Guid>(id);
}
