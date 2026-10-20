using BSX.BuildingBlocks.Authorization;
using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Domain.Organizations;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Crm.Infrastructure.Persistence;

/// <summary>The CRM module persistence context (PostgreSQL; ADR-004).</summary>
public sealed class CrmDbContext : DbContext
{
    private readonly ITenantContext? _tenantContext;

    /// <summary>Initializes a new instance of the <see cref="CrmDbContext"/> class.</summary>
    /// <param name="options">The context options.</param>
    /// <param name="tenantContext">The ambient tenant context, when available.</param>
    public CrmDbContext(DbContextOptions<CrmDbContext> options, ITenantContext? tenantContext = null)
        : base(options)
        => _tenantContext = tenantContext;

    /// <summary>Gets the customers.</summary>
    public DbSet<Customer> Customers => Set<Customer>();

    /// <summary>Gets the organizations.</summary>
    public DbSet<Organization> Organizations => Set<Organization>();

    /// <summary>Gets the ambient tenant used by the global tenant query filters.</summary>
    public Guid? CurrentTenantId => _tenantContext?.TenantId;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        if (Database.ProviderName?.Contains("Npgsql", StringComparison.Ordinal) == true)
        {
            modelBuilder.HasDefaultSchema("crm");
        }

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CrmDbContext).Assembly);

        // Tenant + soft-delete filters. The tenant clause is open only when no ambient tenant is
        // present (background/ops); tenant-scoped repositories always pass an explicit tenant.
        modelBuilder.Entity<Customer>().HasQueryFilter(customer =>
            !EF.Property<bool>(customer, "IsDeleted")
            && (CurrentTenantId == null || EF.Property<Guid>(customer, "TenantKey") == CurrentTenantId));

        modelBuilder.Entity<Organization>().HasQueryFilter(organization =>
            !EF.Property<bool>(organization, "IsDeleted")
            && (CurrentTenantId == null || EF.Property<Guid>(organization, "TenantKey") == CurrentTenantId));

        base.OnModelCreating(modelBuilder);
    }
}
