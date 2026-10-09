using BSX.BuildingBlocks.Authorization;
using BSX.Modules.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BSX.Modules.Identity.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Applies auditing shadow properties, converts hard deletes into soft deletes, and stamps the
/// actor and timestamps (ADR-010). Operates only on entities that declare the shadow properties.
/// </summary>
internal sealed class AuditingSaveChangesInterceptor : SaveChangesInterceptor
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

    /// <summary>Initializes a new instance of the <see cref="AuditingSaveChangesInterceptor"/> class.</summary>
    /// <param name="timeProvider">The time source.</param>
    /// <param name="currentUser">The current principal.</param>
    public AuditingSaveChangesInterceptor(TimeProvider timeProvider, ICurrentUser currentUser)
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

            if (entry.State is EntityState.Added or EntityState.Modified && entry.Entity is User user)
            {
                SetIfPresent(entry, "SearchEmail", user.Email.Value);
                SetIfPresent(entry, "SearchName", user.Name.Value);
            }
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
