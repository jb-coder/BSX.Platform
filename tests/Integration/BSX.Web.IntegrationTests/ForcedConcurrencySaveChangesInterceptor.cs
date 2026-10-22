using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BSX.Web.IntegrationTests;

/// <summary>
/// Deterministically forces an optimistic-concurrency conflict for the customer update whose name
/// is <see cref="TriggerName"/>. It advances the stored row version before EF issues the UPDATE, so
/// the update matches zero rows exactly as it would if another request had saved first.
/// </summary>
internal sealed class ForcedConcurrencySaveChangesInterceptor : SaveChangesInterceptor
{
    /// <summary>The sentinel name that triggers a forced conflict.</summary>
    internal const string TriggerName = "__force-concurrency__";

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is CrmDbContext context)
        {
            var entry = context.ChangeTracker
                .Entries<Customer>()
                .FirstOrDefault(candidate =>
                    candidate.State == EntityState.Modified
                    && candidate.Entity.Name.Value == TriggerName);

            if (entry is not null)
            {
                Guid customerId = entry.Entity.Id.Value;
                await context.Database
                    .ExecuteSqlInterpolatedAsync(
                        $"UPDATE \"customers\" SET \"Version\" = \"Version\" + 1 WHERE \"Id\" = {customerId}",
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
    }
}
