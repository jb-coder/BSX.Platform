using System.Reflection;

namespace BSX.ArchitectureTests;

/// <summary>
/// Provides the assemblies under governance to the architecture test suite.
/// </summary>
internal static class GovernedAssemblies
{
    internal static readonly Assembly SharedKernel = typeof(SharedKernel.Results.Result).Assembly;

    internal static readonly Assembly BuildingBlocks = typeof(BuildingBlocks.Cqrs.ISender).Assembly;

    internal static readonly Assembly[] All = [SharedKernel, BuildingBlocks];
}
