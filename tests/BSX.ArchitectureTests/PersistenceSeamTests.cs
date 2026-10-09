using System.Reflection;
using BSX.BuildingBlocks.Persistence;

namespace BSX.ArchitectureTests;

public sealed class PersistenceSeamTests
{
    [Fact]
    public void UnitOfWork_Should_ExposeTheDispatchSeam()
    {
        MethodInfo? method = typeof(IUnitOfWork).GetMethod("SaveChangesAndDispatchAsync");

        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<int>));
    }

    [Fact]
    public void TransactionBehavior_Should_BeSealedAndInternal()
    {
        Type? behavior = GovernedAssemblies.BuildingBlocks.GetType("BSX.BuildingBlocks.Behaviors.TransactionBehavior`2");

        behavior.Should().NotBeNull();
        behavior!.IsSealed.Should().BeTrue();
        behavior.IsNotPublic.Should().BeTrue();
    }
}
