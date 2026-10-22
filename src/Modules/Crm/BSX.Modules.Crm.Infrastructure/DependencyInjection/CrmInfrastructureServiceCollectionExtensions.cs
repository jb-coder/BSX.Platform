using BSX.BuildingBlocks.Persistence;
using BSX.BuildingBlocks.Persistence.Outbox;
using BSX.Modules.Crm.Application.Abstractions;
using BSX.Modules.Crm.Contracts.References;
using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Domain.Organizations;
using BSX.Modules.Crm.Infrastructure.Persistence;
using BSX.Modules.Crm.Infrastructure.Persistence.Interceptors;
using BSX.Modules.Crm.Infrastructure.References;
using BSX.Modules.Crm.Infrastructure.Repositories;
using BSX.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BSX.Modules.Crm.Infrastructure.DependencyInjection;

/// <summary>Registers the CRM infrastructure layer.</summary>
public static class CrmInfrastructureServiceCollectionExtensions
{
    /// <summary>Registers the CRM DbContext, repositories, contracts and unit of work.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddCrmInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<CrmTenantScope>();
        services.TryAddScoped<ICrmTenantScope>(provider => provider.GetRequiredService<CrmTenantScope>());
        services.TryAddScoped<CrmAuditingSaveChangesInterceptor>();
        services.AddOptions<OutboxOptions>();
        services.TryAddScoped<OutboxSaveChangesInterceptor>();

        services.AddDbContext<CrmDbContext>((provider, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "crm"));
            options.AddInterceptors(
                provider.GetRequiredService<CrmAuditingSaveChangesInterceptor>(),
                provider.GetRequiredService<OutboxSaveChangesInterceptor>());
        });

        services.TryAddScoped<ICustomerRepository, CustomerRepository>();
        services.TryAddScoped<IOrganizationRepository, OrganizationRepository>();
        services.TryAddScoped<ICustomerReadRepository, CustomerReadRepository>();
        services.TryAddScoped<IOrganizationReadRepository, OrganizationReadRepository>();
        services.TryAddScoped<ICrmReferenceLookup, CrmReferenceLookup>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IModuleUnitOfWork, CrmUnitOfWork>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IOutboxStore, EfOutboxStore<CrmDbContext>>());

        return services;
    }
}
