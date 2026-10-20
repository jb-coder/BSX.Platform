using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Domain.Organizations;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Crm.Infrastructure.Persistence;

/// <summary>The CRM module persistence context (PostgreSQL; ADR-004).</summary>
public sealed class CrmDbContext : DbContext
{
    private readonly ICrmTenantScope? _tenantScope;

    /// <summary>Initializes a new instance of the <see cref="CrmDbContext"/> class.</summary>
    /// <param name="options">The context options.</param>
    /// <param name="tenantScope">The tenant scope, when available.</param>
    public CrmDbContext(DbContextOptions<CrmDbContext> options, ICrmTenantScope? tenantScope = null)
        : base(options)
        => _tenantScope = tenantScope;

    /// <summary>Gets the customers.</summary>
    public DbSet<Customer> Customers => Set<Customer>();

    /// <summary>Gets the organizations.</summary>
    public DbSet<Organization> Organizations => Set<Organization>();

    /// <summary>Gets the tenant used by the global query filters.</summary>
    public Guid? CurrentTenantId => _tenantScope?.TenantId;

    /// <summary>Gets a value indicating whether the explicit system scope is active.</summary>
    public bool IsSystemScope => _tenantScope?.IsSystem == true;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        if (Database.ProviderName?.Contains("Npgsql", StringComparison.Ordinal) == true)
        {
            modelBuilder.HasDefaultSchema("crm");
        }

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CrmDbContext).Assembly);

        // Fail-closed tenant filter plus soft delete. A missing tenant returns no data unless the
        // scope is explicitly privileged (system). TenantKey is constrained to TenantId in the DB.
        modelBuilder.Entity<Customer>().HasQueryFilter(customer =>
            !EF.Property<bool>(customer, "IsDeleted")
            && (IsSystemScope || (CurrentTenantId != null && EF.Property<Guid>(customer, "TenantKey") == CurrentTenantId)));

        modelBuilder.Entity<Organization>().HasQueryFilter(organization =>
            !EF.Property<bool>(organization, "IsDeleted")
            && (IsSystemScope || (CurrentTenantId != null && EF.Property<Guid>(organization, "TenantKey") == CurrentTenantId)));

        base.OnModelCreating(modelBuilder);
    }
}
