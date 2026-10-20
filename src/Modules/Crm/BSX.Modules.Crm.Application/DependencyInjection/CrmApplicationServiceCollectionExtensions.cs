using BSX.BuildingBlocks.DependencyInjection;
using BSX.Modules.Crm.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BSX.Modules.Crm.Application.DependencyInjection;

/// <summary>Registers the CRM application layer (handlers, validators and application services).</summary>
public static class CrmApplicationServiceCollectionExtensions
{
    /// <summary>Registers CRM request handlers, validators and tenant resolution.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddCrmApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddBuildingBlocks(typeof(CrmApplicationServiceCollectionExtensions).Assembly);
        services.TryAddScoped<CrmTenantResolver>();
        return services;
    }
}
