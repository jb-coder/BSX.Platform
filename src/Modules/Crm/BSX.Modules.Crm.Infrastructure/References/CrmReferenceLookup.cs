using BSX.Modules.Crm.Contracts.References;
using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.Modules.Crm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Crm.Infrastructure.References;

/// <summary>
/// Tenant-aware implementation of the CRM reference contract used by other modules. It reads only
/// through the CRM persistence context, so soft-deleted references are excluded and the tenant
/// query filter plus the explicit tenant predicate both apply (fail closed).
/// </summary>
public sealed class CrmReferenceLookup : ICrmReferenceLookup
{
    private readonly CrmDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="CrmReferenceLookup"/> class.</summary>
    /// <param name="context">The CRM persistence context.</param>
    public CrmReferenceLookup(CrmDbContext context) => _context = context;

    /// <inheritdoc />
    public Task<bool> CustomerExistsAsync(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        TenantId tenant = TenantId.From(tenantId);
        CustomerId customer = CustomerId.From(customerId);

        return _context.Customers
            .AsNoTracking()
            .AnyAsync(candidate => candidate.TenantId == tenant && candidate.Id == customer, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> OrganizationExistsAsync(
        Guid tenantId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        TenantId tenant = TenantId.From(tenantId);
        OrganizationId organization = OrganizationId.From(organizationId);

        return _context.Organizations
            .AsNoTracking()
            .AnyAsync(candidate => candidate.TenantId == tenant && candidate.Id == organization, cancellationToken);
    }
}
