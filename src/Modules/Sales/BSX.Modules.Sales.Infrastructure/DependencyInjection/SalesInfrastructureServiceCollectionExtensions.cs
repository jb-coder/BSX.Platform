using BSX.BuildingBlocks.Persistence;
using BSX.BuildingBlocks.Persistence.Outbox;
using BSX.Modules.Crm.Contracts.References;
using BSX.Modules.Sales.Application.Abstractions;
using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Domain.Opportunities;
using BSX.Modules.Sales.Infrastructure.Crm;
using BSX.Modules.Sales.Infrastructure.Persistence;
using BSX.Modules.Sales.Infrastructure.Persistence.Interceptors;
using BSX.Modules.Sales.Infrastructure.Repositories;
using BSX.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BSX.Modules.Sales.Infrastructure.DependencyInjection;

/// <summary>Registers the Sales infrastructure layer.</summary>
public static class SalesInfrastructureServiceCollectionExtensions
{
    /// <summary>Registers the Sales DbContext, repositories, validators and unit of work.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddSalesInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<SalesTenantScope>();
        services.TryAddScoped<ISalesTenantScope>(provider => provider.GetRequiredService<SalesTenantScope>());
        services.TryAddScoped<SalesAuditingSaveChangesInterceptor>();
        services.AddOptions<OutboxOptions>();
        services.TryAddScoped<OutboxSaveChangesInterceptor>();

        // Fail closed when CRM is not composed: the lookup rejects every reference. CRM registers
        // its tenant-aware implementation first, so TryAdd keeps the real contract in the host.
        services.TryAddScoped<ICrmReferenceLookup, FailClosedCrmReferenceLookup>();
        services.TryAddScoped<ICrmReferenceValidator, CrmContractReferenceValidator>();
        services.TryAddScoped<CrmReferenceValidationInterceptor>();

        services.AddDbContext<SalesDbContext>((provider, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "sales"));
            options.AddInterceptors(
                provider.GetRequiredService<SalesAuditingSaveChangesInterceptor>(),
                provider.GetRequiredService<CrmReferenceValidationInterceptor>(),
                provider.GetRequiredService<OutboxSaveChangesInterceptor>());
        });

        services.TryAddScoped<ILeadRepository, LeadRepository>();
        services.TryAddScoped<IOpportunityRepository, OpportunityRepository>();
        services.TryAddScoped<ILeadReadRepository, LeadReadRepository>();
        services.TryAddScoped<IOpportunityReadRepository, OpportunityReadRepository>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IModuleUnitOfWork, SalesUnitOfWork>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IOutboxStore, EfOutboxStore<SalesDbContext>>());

        return services;
    }
}
