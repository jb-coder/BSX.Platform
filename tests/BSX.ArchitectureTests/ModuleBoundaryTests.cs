using System.Reflection;
using BSX.BuildingBlocks.Authorization;
using NetArchTest.Rules;

namespace BSX.ArchitectureTests;

/// <summary>
/// Guards the dependency direction defined by ADR-001/ADR-015: a module owns its layers, other
/// modules see only <c>&lt;Module&gt;.Contracts</c>, and no layer leaks infrastructure inward.
/// </summary>
public sealed class ModuleBoundaryTests
{
    private static readonly string[] ForbiddenDomainDependencies =
    [
        "BSX.BuildingBlocks",
        "BSX.Persistence",
        "Microsoft.EntityFrameworkCore",
        "Microsoft.Extensions.DependencyInjection",
        "Npgsql",
        "FluentValidation",
    ];

    private static readonly string[] ForbiddenContractDependencies =
    [
        "BSX.BuildingBlocks",
        "BSX.Persistence",
        "Microsoft.EntityFrameworkCore",
        "Microsoft.Extensions.DependencyInjection",
        "Microsoft.AspNetCore",
        "Npgsql",
        "FluentValidation",
    ];

    [Fact]
    public void Module_Domain_ShouldNotDependOn_InfrastructureOrOtherModules()
    {
        foreach (GovernedAssemblies.ModuleAssemblies module in GovernedAssemblies.Modules)
        {
            string[] forbidden = [.. ForbiddenDomainDependencies, .. GovernedAssemblies.OtherModuleNamespaces(module.Name)];

            TestResult result = Types.InAssembly(module.Domain)
                .ShouldNot()
                .HaveDependencyOnAny(forbidden)
                .GetResult();

            result.IsSuccessful.Should().BeTrue(Failure(module.Name, "Domain", result));
        }
    }

    [Fact]
    public void Module_Contracts_ShouldOnlyDependOn_PlatformContracts()
    {
        foreach (GovernedAssemblies.ModuleAssemblies module in GovernedAssemblies.Modules)
        {
            if (module.Contracts is null)
            {
                continue;
            }

            string[] forbidden =
            [
                .. ForbiddenContractDependencies,
                .. GovernedAssemblies.OtherModuleNamespaces(module.Name),
                $"BSX.Modules.{module.Name}.Domain",
                $"BSX.Modules.{module.Name}.Application",
                $"BSX.Modules.{module.Name}.Infrastructure",
                $"BSX.Modules.{module.Name}.Endpoints",
            ];

            TestResult result = Types.InAssembly(module.Contracts)
                .ShouldNot()
                .HaveDependencyOnAny(forbidden)
                .GetResult();

            result.IsSuccessful.Should().BeTrue(Failure(module.Name, "Contracts", result));
        }
    }

    [Fact]
    public void Module_Application_ShouldNotDependOn_ItsInfrastructureEndpointsOrOtherModules()
    {
        foreach (GovernedAssemblies.ModuleAssemblies module in GovernedAssemblies.Modules)
        {
            string[] forbidden =
            [
                $"BSX.Modules.{module.Name}.Infrastructure",
                $"BSX.Modules.{module.Name}.Endpoints",
                "Microsoft.EntityFrameworkCore",
                "Npgsql",
                .. GovernedAssemblies.OtherModuleNamespaces(module.Name)
                    .Where(name => !name.EndsWith(".Contracts", StringComparison.Ordinal)),
            ];

            TestResult result = Types.InAssembly(module.Application)
                .ShouldNot()
                .HaveDependencyOnAny(forbidden)
                .GetResult();

            result.IsSuccessful.Should().BeTrue(Failure(module.Name, "Application", result));
        }
    }

    [Fact]
    public void Module_Endpoints_ShouldNotDependOn_ItsDomainOrOtherModuleImplementations()
    {
        foreach (GovernedAssemblies.ModuleAssemblies module in GovernedAssemblies.Modules)
        {
            string[] forbidden =
            [
                $"BSX.Modules.{module.Name}.Domain",
                .. GovernedAssemblies.OtherModuleNamespaces(module.Name)
                    .Where(name => !name.EndsWith(".Contracts", StringComparison.Ordinal)),
            ];

            TestResult result = Types.InAssembly(module.Endpoints)
                .ShouldNot()
                .HaveDependencyOnAny(forbidden)
                .GetResult();

            result.IsSuccessful.Should().BeTrue(Failure(module.Name, "Endpoints", result));
        }
    }

    [Fact]
    public void Module_Infrastructure_ShouldNotDependOn_OtherModuleImplementations()
    {
        foreach (GovernedAssemblies.ModuleAssemblies module in GovernedAssemblies.Modules)
        {
            string[] forbidden = GovernedAssemblies.OtherModuleNamespaces(module.Name)
                .Where(name => !name.EndsWith(".Contracts", StringComparison.Ordinal))
                .ToArray();

            TestResult result = Types.InAssembly(module.Infrastructure)
                .ShouldNot()
                .HaveDependencyOnAny(forbidden)
                .GetResult();

            result.IsSuccessful.Should().BeTrue(Failure(module.Name, "Infrastructure", result));
        }
    }

    [Fact]
    public void Contracts_ShouldNotDependOn_ImplementationLayers()
    {
        TestResult result = Types.InAssembly(GovernedAssemblies.Contracts)
            .ShouldNot()
            .HaveDependencyOnAny(
                "BSX.BuildingBlocks",
                "BSX.Persistence",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.Extensions.DependencyInjection",
                "Microsoft.AspNetCore",
                "Npgsql",
                "FluentValidation")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Failure("Platform", "Contracts", result));
    }

    [Fact]
    public void Persistence_ShouldNotDependOn_AnyModule()
    {
        TestResult result = Types.InAssembly(GovernedAssemblies.Persistence)
            .ShouldNot()
            .HaveDependencyOn("BSX.Modules")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Failure("Platform", "Persistence", result));
    }

    [Fact]
    public void EveryDeclaredPermission_ShouldExistInThePlatformCatalog()
    {
        HashSet<string> catalog = GetCatalogCodes();

        foreach (GovernedAssemblies.ModuleAssemblies module in GovernedAssemblies.Modules)
        {
            IEnumerable<string> declared = module.Application
                .GetTypes()
                .SelectMany(type => type.GetCustomAttributes<RequirePermissionAttribute>(inherit: true))
                .Select(attribute => attribute.Permission);

            declared.Should().OnlyContain(
                code => catalog.Contains(code),
                "every [RequirePermission] must be published by a module permission catalog (ADR-018)");
        }
    }

    private static HashSet<string> GetCatalogCodes()
    {
        var sources = new BSX.Contracts.Permissions.IPermissionCatalogSource[]
        {
            new Modules.Identity.Application.Common.IdentityPermissionCatalog(),
            new Modules.Crm.Contracts.Permissions.CrmPermissionCatalog(),
            new Modules.Sales.Contracts.Permissions.SalesPermissionCatalog(),
        };

        return new BSX.Contracts.Permissions.PermissionCatalog(sources)
            .Permissions
            .Select(permission => permission.Code)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static string Failure(string module, string layer, TestResult result)
        => $"{module}.{layer} violates the module boundary. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}";
}
