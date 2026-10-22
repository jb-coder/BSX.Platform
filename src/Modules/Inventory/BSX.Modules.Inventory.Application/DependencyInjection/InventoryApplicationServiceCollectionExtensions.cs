using BSX.BuildingBlocks.DependencyInjection;
using BSX.Contracts.Permissions;
using BSX.Modules.Inventory.Application.Abstractions;
using BSX.Modules.Inventory.Application.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BSX.Modules.Inventory.Application.DependencyInjection;

/// <summary>Registers the Inventory application layer (handlers, validators and services).</summary>
public static class InventoryApplicationServiceCollectionExtensions
{
    /// <summary>Registers Inventory request handlers, validators, tenant resolution and permission catalog.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddInventoryApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddBuildingBlocks(typeof(InventoryApplicationServiceCollectionExtensions).Assembly);
        services.TryAddScoped<InventoryTenantResolver>();
        services.TryAddScoped<InventoryStockPolicy>();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPermissionCatalogSource, InventoryPermissionCatalog>());
        return services;
    }
}
