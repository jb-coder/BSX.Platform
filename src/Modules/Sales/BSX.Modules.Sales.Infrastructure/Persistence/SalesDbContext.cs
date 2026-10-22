using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Domain.Opportunities;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Sales.Infrastructure.Persistence;

/// <summary>The Sales module persistence context (PostgreSQL; ADR-004).</summary>
public sealed class SalesDbContext : DbContext
{
    private readonly ISalesTenantScope? _tenantScope;

    /// <summary>Initializes a new instance of the <see cref="SalesDbContext"/> class.</summary>
    /// <param name="options">The context options.</param>
    /// <param name="tenantScope">The tenant scope, when available.</param>
    public SalesDbContext(DbContextOptions<SalesDbContext> options, ISalesTenantScope? tenantScope = null)
        : base(options)
        => _tenantScope = tenantScope;

    /// <summary>Gets the leads.</summary>
    public DbSet<Lead> Leads => Set<Lead>();

    /// <summary>Gets the opportunities.</summary>
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();

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
            modelBuilder.HasDefaultSchema("sales");
        }

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SalesDbContext).Assembly);

        // Fail-closed tenant filter plus soft delete. A missing tenant returns no data unless the
        // scope is explicitly privileged (system). TenantKey is constrained to TenantId in the DB.
        modelBuilder.Entity<Lead>().HasQueryFilter(lead =>
            !EF.Property<bool>(lead, "IsDeleted")
            && (IsSystemScope || (CurrentTenantId != null && EF.Property<Guid>(lead, "TenantKey") == CurrentTenantId)));

        modelBuilder.Entity<Opportunity>().HasQueryFilter(opportunity =>
            !EF.Property<bool>(opportunity, "IsDeleted")
            && (IsSystemScope || (CurrentTenantId != null && EF.Property<Guid>(opportunity, "TenantKey") == CurrentTenantId)));

        base.OnModelCreating(modelBuilder);
    }
}
