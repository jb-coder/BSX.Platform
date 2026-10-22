using BSX.BuildingBlocks.DependencyInjection;
using BSX.Contracts.Permissions;
using BSX.Modules.Sales.Application.Abstractions;
using BSX.Modules.Sales.Contracts.Permissions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BSX.Modules.Sales.Application.DependencyInjection;

/// <summary>Registers the Sales application layer (handlers, validators and application services).</summary>
public static class SalesApplicationServiceCollectionExtensions
{
    /// <summary>Registers Sales request handlers, validators, tenant resolution and permission catalog.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddSalesApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddBuildingBlocks(typeof(SalesApplicationServiceCollectionExtensions).Assembly);
        services.TryAddScoped<SalesTenantResolver>();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPermissionCatalogSource, SalesPermissionCatalog>());
        return services;
    }
}
