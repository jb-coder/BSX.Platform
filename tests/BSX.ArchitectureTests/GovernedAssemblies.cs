using System.Reflection;

namespace BSX.ArchitectureTests;

/// <summary>
/// Provides the platform and module assemblies under governance to the architecture test suite.
/// </summary>
internal static class GovernedAssemblies
{
    internal static readonly Assembly SharedKernel = typeof(SharedKernel.Results.Result).Assembly;

    internal static readonly Assembly Contracts = typeof(Contracts.Permissions.PermissionCatalog).Assembly;

    internal static readonly Assembly BuildingBlocks = typeof(BuildingBlocks.Cqrs.ISender).Assembly;

    internal static readonly Assembly Persistence = typeof(BSX.Persistence.Outbox.OutboxMessage).Assembly;

    internal static readonly Assembly[] Platform = [SharedKernel, Contracts, BuildingBlocks, Persistence];

    internal static readonly ModuleAssemblies Identity = new(
        Name: "Identity",
        Domain: typeof(Modules.Identity.Domain.Users.User).Assembly,
        Application: typeof(Modules.Identity.Application.Authentication.PermissionResolver).Assembly,
        Infrastructure: typeof(Modules.Identity.Infrastructure.Persistence.IdentityDbContext).Assembly,
        Endpoints: typeof(Modules.Identity.Endpoints.IdentityEndpointRouteBuilderExtensions).Assembly,
        Contracts: null);

    internal static readonly ModuleAssemblies Crm = new(
        Name: "Crm",
        Domain: typeof(Modules.Crm.Domain.Customers.Customer).Assembly,
        Application: typeof(Modules.Crm.Application.Abstractions.CrmTenantResolver).Assembly,
        Infrastructure: typeof(Modules.Crm.Infrastructure.Persistence.CrmDbContext).Assembly,
        Endpoints: typeof(Modules.Crm.Endpoints.CrmEndpointRouteBuilderExtensions).Assembly,
        Contracts: typeof(Modules.Crm.Contracts.Permissions.CrmPermissionCatalog).Assembly);

    internal static readonly ModuleAssemblies Sales = new(
        Name: "Sales",
        Domain: typeof(Modules.Sales.Domain.Leads.Lead).Assembly,
        Application: typeof(Modules.Sales.Application.Abstractions.SalesTenantResolver).Assembly,
        Infrastructure: typeof(Modules.Sales.Infrastructure.Persistence.SalesDbContext).Assembly,
        Endpoints: typeof(Modules.Sales.Endpoints.SalesEndpointRouteBuilderExtensions).Assembly,
        Contracts: typeof(Modules.Sales.Contracts.Permissions.SalesPermissionCatalog).Assembly);

    // Inventory is currently Domain-only; later layers are added without changing the governance model.
    internal static readonly ModuleAssemblies Inventory = new(
        Name: "Inventory",
        Domain: typeof(Modules.Inventory.Domain.Products.Product).Assembly,
        Application: null,
        Infrastructure: null,
        Endpoints: null,
        Contracts: null);

    internal static readonly ModuleAssemblies[] Modules = [Identity, Crm, Sales, Inventory];

    /// <summary>Assemblies subject to naming and sealing conventions (technical platform only).</summary>
    internal static readonly Assembly[] All = Platform;

    internal static string[] OtherModuleNamespaces(string moduleName) => Modules
        .Where(module => !string.Equals(module.Name, moduleName, StringComparison.Ordinal))
        .SelectMany(module => module.Namespaces)
        .Distinct(StringComparer.Ordinal)
        .ToArray();

    internal sealed record ModuleAssemblies(
        string Name,
        Assembly Domain,
        Assembly? Application,
        Assembly? Infrastructure,
        Assembly? Endpoints,
        Assembly? Contracts)
    {
        public IReadOnlyCollection<Assembly> All => new[] { Domain, Application, Infrastructure, Endpoints, Contracts }
            .Where(assembly => assembly is not null)
            .Cast<Assembly>()
            .ToArray();

        public IReadOnlyCollection<string> Namespaces => new[]
        {
            $"BSX.Modules.{Name}.Domain",
            $"BSX.Modules.{Name}.Application",
            $"BSX.Modules.{Name}.Infrastructure",
            $"BSX.Modules.{Name}.Endpoints",
            $"BSX.Modules.{Name}.Contracts",
        };
    }
}
