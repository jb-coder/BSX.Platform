using BSX.BuildingBlocks.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BSX.BuildingBlocks.DependencyInjection;

/// <summary>Registers the platform composite unit of work that composes the module units.</summary>
public static class CompositeUnitOfWorkServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="CompositeUnitOfWork"/> as the single <see cref="IUnitOfWork"/> used by
    /// the command pipeline. Call it after the module infrastructure registrations, so the module
    /// units of work are available.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddCompositeUnitOfWork(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<CompositeUnitOfWork>();
        services.TryAddScoped<IUnitOfWork>(provider => provider.GetRequiredService<CompositeUnitOfWork>());
        return services;
    }
}
