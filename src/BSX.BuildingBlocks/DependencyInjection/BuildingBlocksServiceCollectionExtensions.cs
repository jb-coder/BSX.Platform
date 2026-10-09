using System.Reflection;
using BSX.BuildingBlocks.Behaviors;
using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Events;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BSX.BuildingBlocks.DependencyInjection;

/// <summary>
/// Registers the technical building blocks of the platform.
/// </summary>
public static class BuildingBlocksServiceCollectionExtensions
{
    /// <summary>
    /// Registers the CQRS dispatcher, the domain event dispatcher, the pipeline behaviors and
    /// the request handlers, domain event handlers and validators found in the supplied assemblies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="handlerAssemblies">The assemblies that contain handlers and validators.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddBuildingBlocks(this IServiceCollection services, params Assembly[] handlerAssemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(handlerAssemblies);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<ISender, Sender>();
        services.TryAddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>)));
        services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>)));

        foreach (Assembly assembly in handlerAssemblies)
        {
            services.RegisterRequestHandlers(assembly);
            services.RegisterDomainEventHandlers(assembly);
            services.AddValidatorsFromAssembly(assembly);
        }

        return services;
    }

    private static void RegisterRequestHandlers(this IServiceCollection services, Assembly assembly)
    {
        foreach (Type implementationType in GetConcreteTypes(assembly))
        {
            foreach (Type serviceType in implementationType
                .GetInterfaces()
                .Where(static type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)))
            {
                services.TryAddEnumerable(ServiceDescriptor.Scoped(serviceType, implementationType));
            }
        }
    }

    private static void RegisterDomainEventHandlers(this IServiceCollection services, Assembly assembly)
    {
        foreach (Type implementationType in GetConcreteTypes(assembly))
        {
            foreach (Type serviceType in implementationType
                .GetInterfaces()
                .Where(static type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>)))
            {
                services.TryAddEnumerable(ServiceDescriptor.Scoped(serviceType, implementationType));
            }
        }
    }

    private static IEnumerable<Type> GetConcreteTypes(Assembly assembly) => assembly
        .GetTypes()
        .Where(static type => type is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false });
}
