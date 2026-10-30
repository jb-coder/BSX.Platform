using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Persistence;
using BSX.BuildingBlocks.Persistence.Outbox;
using BSX.Contracts.Permissions;
using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Application.Authentication;
using BSX.Modules.Identity.Domain.Memberships;
using BSX.Modules.Identity.Domain.Roles;
using BSX.Modules.Identity.Domain.ServiceAccounts;
using BSX.Modules.Identity.Domain.Sessions;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Infrastructure.Persistence;
using BSX.Modules.Identity.Infrastructure.Authentication;
using BSX.Modules.Identity.Infrastructure.Persistence.Interceptors;
using BSX.Modules.Identity.Infrastructure.Persistence.Outbox;
using BSX.Modules.Identity.Infrastructure.Permissions;
using BSX.Modules.Identity.Infrastructure.Repositories;
using BSX.Modules.Identity.Infrastructure.Security;
using BSX.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BSX.Modules.Identity.Infrastructure.DependencyInjection;

/// <summary>Registers the Identity infrastructure layer.</summary>
public static class IdentityInfrastructureServiceCollectionExtensions
{
    /// <summary>Registers the Identity DbContext, repositories, security services and unit of work.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.TryAddScoped<ICurrentUser, SystemCurrentUser>();
        services.TryAddScoped<AuditingSaveChangesInterceptor>();
        services.AddOptions<OutboxOptions>();
        services.TryAddScoped<OutboxSaveChangesInterceptor>();

        services.AddDbContext<IdentityDbContext>((provider, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "identity"));
            options.AddInterceptors(
                provider.GetRequiredService<AuditingSaveChangesInterceptor>(),
                provider.GetRequiredService<OutboxSaveChangesInterceptor>());
        });

        services.TryAddScoped<IUserRepository, UserRepository>();
        services.TryAddScoped<IRoleRepository, RoleRepository>();
        services.TryAddScoped<ITenantMembershipRepository, TenantMembershipRepository>();
        services.TryAddScoped<IServiceAccountRepository, ServiceAccountRepository>();
        services.TryAddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.TryAddScoped<IUserSessionRepository, UserSessionRepository>();
        services.TryAddScoped<IAuthorizationSnapshotReader, AuthorizationSnapshotReader>();
        services.TryAddScoped<IUserReadRepository, UserReadRepository>();
        services.TryAddScoped<IRoleReadRepository, RoleReadRepository>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IModuleUnitOfWork, IdentityUnitOfWork>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IOutboxStore, EfOutboxStore<IdentityDbContext>>());
        services.TryAddScoped<IOutboxWriter, OutboxWriter>();
        services.TryAddScoped<SystemRolePermissionSynchronizer>();
        services.TryAddSingleton<PermissionCatalog>();

        services.TryAddSingleton<IPasswordHasher, PasswordHasher>();
        services.TryAddSingleton<IApiKeyHasher, ApiKeyHasher>();
        services.TryAddSingleton<ISecureTokenGenerator, SecureTokenGenerator>();
        services.TryAddSingleton<IRefreshTokenHasher, RefreshTokenHasher>();
        services.TryAddSingleton<IPasswordResetTokenHasher, PasswordResetTokenHasher>();
        services.TryAddSingleton<IPasswordResetNotifier, DisabledPasswordResetNotifier>();

        services.TryAddSingleton<RateLimitOptions>();
        services.TryAddSingleton<IRateLimiter, InMemoryRateLimiter>();

        return services;
    }
}
