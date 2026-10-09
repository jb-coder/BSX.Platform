using NetArchTest.Rules;

namespace BSX.ArchitectureTests;

public sealed class LayerDependencyTests
{
    [Fact]
    public void SharedKernel_ShouldNot_DependOn_BuildingBlocks()
    {
        TestResult result = Types.InAssembly(GovernedAssemblies.SharedKernel)
            .ShouldNot()
            .HaveDependencyOn("BSX.BuildingBlocks")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureMessage(result));
    }

    [Fact]
    public void SharedKernel_ShouldNot_DependOn_InfrastructurePackages()
    {
        TestResult result = Types.InAssembly(GovernedAssemblies.SharedKernel)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Npgsql", "FluentValidation")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureMessage(result));
    }

    [Fact]
    public void BuildingBlocks_ShouldNot_DependOn_EntityFramework()
    {
        TestResult result = Types.InAssembly(GovernedAssemblies.BuildingBlocks)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Npgsql")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureMessage(result));
    }

    private static string FailureMessage(TestResult result)
        => $"Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}";
}
