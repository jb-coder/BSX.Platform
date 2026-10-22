using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BSX.Web.IntegrationTests;

/// <summary>
/// Deterministically forces an optimistic-concurrency conflict for the lead update whose title is
/// <see cref="TriggerTitle"/>. It advances the stored row version before EF issues the UPDATE, so
/// the update matches zero rows exactly as it would if another request had saved first.
/// </summary>
internal sealed class ForcedSalesConcurrencySaveChangesInterceptor : SaveChangesInterceptor
{
    /// <summary>The sentinel title that triggers a forced conflict.</summary>
    internal const string TriggerTitle = "__force-sales-concurrency__";

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is SalesDbContext context)
        {
            var entry = context.ChangeTracker
                .Entries<Lead>()
                .FirstOrDefault(candidate =>
                    candidate.State == EntityState.Modified
                    && candidate.Entity.Title.Value == TriggerTitle);

            if (entry is not null)
            {
                Guid leadId = entry.Entity.Id.Value;
                await context.Database
                    .ExecuteSqlInterpolatedAsync(
                        $"UPDATE \"leads\" SET \"Version\" = \"Version\" + 1 WHERE \"Id\" = {leadId}",
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
    }
}
