using NetArchTest.Rules;

namespace BSX.ArchitectureTests;

public sealed class ClassSealingTests
{
    [Fact]
    public void ConcreteClasses_Should_BeSealed()
    {
        foreach (var assembly in GovernedAssemblies.All)
        {
            TestResult result = Types.InAssembly(assembly)
                .That()
                .AreClasses()
                .And()
                .AreNotAbstract()
                .And()
                .DoNotHaveName("Result")
                .Should()
                .BeSealed()
                .GetResult();

            result.IsSuccessful.Should().BeTrue($"Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
    }
}
