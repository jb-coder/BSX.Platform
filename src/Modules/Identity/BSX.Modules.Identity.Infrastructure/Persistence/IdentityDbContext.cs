using BSX.Modules.Identity.Domain.Memberships;
using BSX.Modules.Identity.Domain.Roles;
using BSX.Modules.Identity.Domain.ServiceAccounts;
using BSX.Modules.Identity.Domain.Sessions;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Identity.Infrastructure.Persistence;

/// <summary>The Identity module persistence context (PostgreSQL; ADR-004).</summary>
public sealed class IdentityDbContext : DbContext
{
    /// <summary>Initializes a new instance of the <see cref="IdentityDbContext"/> class.</summary>
    /// <param name="options">The context options.</param>
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    /// <summary>Gets the users.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Gets the roles.</summary>
    public DbSet<Role> Roles => Set<Role>();

    /// <summary>Gets the tenant memberships.</summary>
    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();

    /// <summary>Gets the service accounts.</summary>
    public DbSet<ServiceAccount> ServiceAccounts => Set<ServiceAccount>();

    /// <summary>Gets the API keys.</summary>
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    /// <summary>Gets the user sessions (ADR-017).</summary>
    public DbSet<UserSession> Sessions => Set<UserSession>();

    /// <summary>Gets the transactional outbox (ADR-009).</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        if (Database.ProviderName?.Contains("Npgsql", StringComparison.Ordinal) == true)
        {
            modelBuilder.HasDefaultSchema("identity");
        }

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
