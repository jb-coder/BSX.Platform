using NetArchTest.Rules;

namespace BSX.ArchitectureTests;

public sealed class NamingConventionTests
{
    [Fact]
    public void Interfaces_Should_BePrefixedWith_I()
    {
        foreach (var assembly in GovernedAssemblies.All)
        {
            TestResult result = Types.InAssembly(assembly)
                .That()
                .AreInterfaces()
                .And()
                .ArePublic()
                .Should()
                .HaveNameStartingWith("I")
                .GetResult();

            result.IsSuccessful.Should().BeTrue($"Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
    }
}
