using BSX.BuildingBlocks.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

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
        return services;
    }
}
