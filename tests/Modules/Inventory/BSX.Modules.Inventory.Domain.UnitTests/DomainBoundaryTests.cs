using System.Reflection;
using BSX.Modules.Inventory.Domain.Products;

namespace BSX.Modules.Inventory.Domain.UnitTests;

/// <summary>Enforces the Domain isolation rule (module-structure, ADR-003): Domain references only SharedKernel.</summary>
public sealed class DomainBoundaryTests
{
    [Fact]
    public void Domain_Should_Not_Reference_Infrastructure_Application_Or_OtherModules()
    {
        Assembly domain = typeof(Product).Assembly;

        string[] forbidden = domain.GetReferencedAssemblies()
            .Select(assembly => assembly.Name ?? string.Empty)
            .Where(name => name.Contains("EntityFrameworkCore", StringComparison.Ordinal)
                || name.Contains("Npgsql", StringComparison.Ordinal)
                || name == "BSX.BuildingBlocks"
                || name == "BSX.Contracts"
                || name == "BSX.Persistence"
                || name.StartsWith("BSX.Modules.", StringComparison.Ordinal))
            .ToArray();

        forbidden.Should().BeEmpty();
    }

    [Fact]
    public void Domain_Should_Reference_SharedKernel()
    {
        typeof(Product).Assembly.GetReferencedAssemblies()
            .Select(assembly => assembly.Name)
            .Should().Contain("BSX.SharedKernel");
    }
}
