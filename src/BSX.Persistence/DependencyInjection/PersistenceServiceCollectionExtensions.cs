using BSX.Persistence.Outbox;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BSX.Persistence.DependencyInjection;

/// <summary>Registers platform persistence services, including outbox processing (ADR-009).</summary>
public static class PersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Registers the outbox interceptor and the crash-recovery hosted service. Call it after the
    /// module infrastructure registrations so every module outbox store is available.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddPlatformOutbox(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<OutboxOptions>()
            .Bind(configuration.GetSection(OutboxOptions.SectionName));
        services.TryAddScoped<OutboxSaveChangesInterceptor>();
        services.AddHostedService<OutboxRecoveryService>();
        return services;
    }
}
