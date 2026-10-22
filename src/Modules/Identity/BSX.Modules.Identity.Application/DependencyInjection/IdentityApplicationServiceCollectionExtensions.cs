using BSX.BuildingBlocks.DependencyInjection;
using BSX.Contracts.Permissions;
using BSX.Modules.Identity.Application.Authentication;
using BSX.Modules.Identity.Application.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BSX.Modules.Identity.Application.DependencyInjection;

/// <summary>Registers the Identity application layer.</summary>
public static class IdentityApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Identity request handlers, domain event handlers and validators, together with
    /// the platform building blocks.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddBuildingBlocks(typeof(IdentityApplicationServiceCollectionExtensions).Assembly);
        services.TryAddScoped<PermissionResolver>();
        services.TryAddScoped<TenantGuard>();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPermissionCatalogSource, IdentityPermissionCatalog>());
        return services;
    }
}
