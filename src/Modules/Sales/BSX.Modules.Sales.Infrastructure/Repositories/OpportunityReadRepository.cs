using BSX.Modules.Sales.Application.Abstractions;
using BSX.Modules.Sales.Application.Dtos;
using BSX.Modules.Sales.Domain.Opportunities;
using BSX.Modules.Sales.Domain.ValueObjects;
using BSX.Modules.Sales.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Sales.Infrastructure.Repositories;

/// <summary>
/// Projection-based opportunity reads; list queries never load aggregates. The search term matches
/// the title, or the customer id when the term parses as a GUID.
/// </summary>
internal sealed class OpportunityReadRepository : IOpportunityReadRepository
{
    private const int DefaultSize = 20;
    private const int MaxSize = 100;

    private readonly SalesDbContext _context;

    public OpportunityReadRepository(SalesDbContext context) => _context = context;

    public async Task<OpportunityDto?> GetByIdAsync(
        Guid tenantId,
        Guid opportunityId,
        CancellationToken cancellationToken = default)
    {
        TenantId tenant = TenantId.From(tenantId);
        OpportunityId id = OpportunityId.From(opportunityId);

        var row = await _context.Opportunities
            .AsNoTracking()
            .Where(opportunity => opportunity.TenantId == tenant && opportunity.Id == id)
            .Select(opportunity => new
            {
                opportunity.Id,
                opportunity.TenantId,
                opportunity.LeadId,
                opportunity.Title,
                opportunity.CustomerId,
                opportunity.OrganizationId,
                opportunity.Amount,
                opportunity.Probability,
                opportunity.ExpectedCloseDate,
                opportunity.Stage,
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (row is null)
        {
            return null;
        }

        decimal weightedAmount = row.Amount.ApplyPercentage(row.Probability.Percent).Amount;

        return new OpportunityDto(
            row.Id.Value,
            row.TenantId.Value,
            row.LeadId.Value,
            row.Title.Value,
            row.CustomerId.Value,
            row.OrganizationId?.Value,
            row.Amount.Amount,
            row.Amount.Currency,
            row.Probability.Percent,
            row.ExpectedCloseDate.Value,
            row.Stage.ToString(),
            weightedAmount);
    }

    public async Task<PagedResult<OpportunitySummaryDto>> ListAsync(
        Guid tenantId,
        int page,
        int size,
        string? search,
        CancellationToken cancellationToken = default)
    {
        int normalizedPage = page < 1 ? 1 : page;
        int normalizedSize = size is < 1 or > MaxSize ? DefaultSize : size;
        TenantId tenant = TenantId.From(tenantId);

        IQueryable<Opportunity> query = _context.Opportunities
            .AsNoTracking()
            .Where(opportunity => opportunity.TenantId == tenant);

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim();
            string pattern = $"%{term.ToLowerInvariant()}%";

            if (Guid.TryParse(term, out Guid customerId) && customerId != Guid.Empty)
            {
                CustomerId customer = CustomerId.From(customerId);
                query = query.Where(opportunity =>
                    opportunity.CustomerId == customer
                    || EF.Functions.Like(EF.Property<string>(opportunity, "SearchTitle"), pattern));
            }
            else
            {
                query = query.Where(opportunity =>
                    EF.Functions.Like(EF.Property<string>(opportunity, "SearchTitle"), pattern));
            }
        }

        int total = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var rows = await query
            .OrderBy(opportunity => EF.Property<string>(opportunity, "SearchTitle"))
            .ThenBy(opportunity => opportunity.Id)
            .Skip((normalizedPage - 1) * normalizedSize)
            .Take(normalizedSize)
            .Select(opportunity => new
            {
                opportunity.Id,
                opportunity.LeadId,
                opportunity.Title,
                opportunity.CustomerId,
                opportunity.OrganizationId,
                opportunity.Amount,
                opportunity.Probability,
                opportunity.ExpectedCloseDate,
                opportunity.Stage,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        List<OpportunitySummaryDto> items = rows
            .Select(row => new OpportunitySummaryDto(
                row.Id.Value,
                row.LeadId.Value,
                row.Title.Value,
                row.CustomerId.Value,
                row.OrganizationId?.Value,
                row.Amount.Amount,
                row.Amount.Currency,
                row.Probability.Percent,
                row.ExpectedCloseDate.Value,
                row.Stage.ToString(),
                row.Amount.ApplyPercentage(row.Probability.Percent).Amount))
            .ToList();

        return new PagedResult<OpportunitySummaryDto>(items, normalizedPage, normalizedSize, total);
    }
}
