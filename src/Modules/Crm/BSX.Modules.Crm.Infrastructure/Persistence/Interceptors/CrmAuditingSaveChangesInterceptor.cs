using BSX.BuildingBlocks.Authorization;
using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BSX.Modules.Crm.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Applies CRM auditing shadow properties, converts hard deletes into soft deletes, maintains the
/// denormalized search columns, and advances the optimistic concurrency token (ADR-010).
/// </summary>
internal sealed class CrmAuditingSaveChangesInterceptor : SaveChangesInterceptor
{
    private const string CreatedOnUtc = "CreatedOnUtc";
    private const string CreatedBy = "CreatedBy";
    private const string UpdatedOnUtc = "UpdatedOnUtc";
    private const string UpdatedBy = "UpdatedBy";
    private const string DeletedOnUtc = "DeletedOnUtc";
    private const string DeletedBy = "DeletedBy";
    private const string IsDeleted = "IsDeleted";
    private const string AnonymousActor = "system";

    private readonly TimeProvider _timeProvider;
    private readonly ICurrentUser _currentUser;

    /// <summary>Initializes a new instance of the <see cref="CrmAuditingSaveChangesInterceptor"/> class.</summary>
    /// <param name="timeProvider">The time source.</param>
    /// <param name="currentUser">The current principal.</param>
    public CrmAuditingSaveChangesInterceptor(TimeProvider timeProvider, ICurrentUser currentUser)
    {
        _timeProvider = timeProvider;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        DateTimeOffset nowUtc = _timeProvider.GetUtcNow();
        string actor = _currentUser.UserId?.ToString() ?? AnonymousActor;

        foreach (EntityEntry entry in context.ChangeTracker.Entries())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    SetIfPresent(entry, CreatedOnUtc, nowUtc);
                    SetIfPresent(entry, CreatedBy, actor);
                    SetIfPresent(entry, UpdatedOnUtc, nowUtc);
                    SetIfPresent(entry, UpdatedBy, actor);
                    break;

                case EntityState.Modified:
                    SetIfPresent(entry, UpdatedOnUtc, nowUtc);
                    SetIfPresent(entry, UpdatedBy, actor);
                    break;

                case EntityState.Deleted:
                    if (entry.Metadata.FindProperty(IsDeleted) is not null)
                    {
                        entry.Property(IsDeleted).CurrentValue = true;
                        SetIfPresent(entry, DeletedOnUtc, nowUtc);
                        SetIfPresent(entry, DeletedBy, actor);
                        entry.State = EntityState.Modified;
                    }

                    break;
            }

            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                ApplyComputedColumns(entry);
            }

            if (entry.Metadata.FindProperty("Version") is not null)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Property("Version").CurrentValue = 1;
                }
                else if (entry.State == EntityState.Modified)
                {
                    int originalValue = entry.Property("Version").OriginalValue is int value ? value : 0;
                    entry.Property("Version").CurrentValue = originalValue + 1;
                }
            }
        }
    }

    private static void ApplyComputedColumns(EntityEntry entry)
    {
        switch (entry.Entity)
        {
            case Customer customer:
                SetIfPresent(entry, "TenantKey", customer.TenantId.Value);
                SetIfPresent(entry, "SearchCode", customer.Code.Value.ToLowerInvariant());
                SetIfPresent(entry, "SearchName", customer.Name.Value.ToLowerInvariant());
                break;

            case Organization organization:
                SetIfPresent(entry, "TenantKey", organization.TenantId.Value);
                SetIfPresent(entry, "SearchName", organization.Name.Value.ToLowerInvariant());
                SetIfPresent(
                    entry,
                    "SearchTaxIdentifier",
                    organization.TaxIdentifier?.Value.ToLowerInvariant() ?? string.Empty);
                break;
        }
    }

    private static void SetIfPresent(EntityEntry entry, string propertyName, object value)
    {
        if (entry.Metadata.FindProperty(propertyName) is not null)
        {
            entry.Property(propertyName).CurrentValue = value;
        }
    }
}
