using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Domain.Opportunities;
using BSX.Modules.Sales.Infrastructure.Crm;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BSX.Modules.Sales.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Validates CRM customer and organization references before Sales aggregates are persisted. The
/// actual lookup is delegated to <see cref="ICrmReferenceValidator"/> so the module never references
/// the CRM domain or database directly.
/// </summary>
internal sealed class CrmReferenceValidationInterceptor : SaveChangesInterceptor
{
    private readonly ICrmReferenceValidator _validator;

    /// <summary>Initializes a new instance of the <see cref="CrmReferenceValidationInterceptor"/> class.</summary>
    /// <param name="validator">The CRM reference validator.</param>
    public CrmReferenceValidationInterceptor(ICrmReferenceValidator validator) => _validator = validator;

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ValidateAsync(eventData.Context, CancellationToken.None).GetAwaiter().GetResult();
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(eventData.Context, cancellationToken).ConfigureAwait(false);
        return await base.SavingChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
    }

    private async Task ValidateAsync(DbContext? context, CancellationToken cancellationToken)
    {
        if (context is null)
        {
            return;
        }

        foreach (EntityEntry<Lead> entry in context.ChangeTracker.Entries<Lead>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified)
                || entry.Property("IsDeleted").CurrentValue is true)
            {
                continue;
            }

            Lead lead = entry.Entity;

            if (lead.CustomerId is not null
                && !await _validator.CustomerExistsAsync(lead.TenantId, lead.CustomerId, cancellationToken)
                    .ConfigureAwait(false))
            {
                throw new CrmReferenceValidationException(
                    "Customer",
                    lead.CustomerId.Value,
                    lead.TenantId.Value);
            }

            if (lead.OrganizationId is not null
                && !await _validator.OrganizationExistsAsync(
                        lead.TenantId,
                        lead.OrganizationId,
                        cancellationToken)
                    .ConfigureAwait(false))
            {
                throw new CrmReferenceValidationException(
                    "Organization",
                    lead.OrganizationId.Value,
                    lead.TenantId.Value);
            }
        }

        foreach (EntityEntry<Opportunity> entry in context.ChangeTracker.Entries<Opportunity>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified)
                || entry.Property("IsDeleted").CurrentValue is true)
            {
                continue;
            }

            Opportunity opportunity = entry.Entity;

            if (!await _validator.CustomerExistsAsync(
                    opportunity.TenantId,
                    opportunity.CustomerId,
                    cancellationToken)
                .ConfigureAwait(false))
            {
                throw new CrmReferenceValidationException(
                    "Customer",
                    opportunity.CustomerId.Value,
                    opportunity.TenantId.Value);
            }

            if (opportunity.OrganizationId is not null
                && !await _validator.OrganizationExistsAsync(
                        opportunity.TenantId,
                        opportunity.OrganizationId,
                        cancellationToken)
                    .ConfigureAwait(false))
            {
                throw new CrmReferenceValidationException(
                    "Organization",
                    opportunity.OrganizationId.Value,
                    opportunity.TenantId.Value);
            }
        }
    }
}
