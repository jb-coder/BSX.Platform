using BSX.Modules.Sales.Application.Abstractions;
using BSX.Modules.Sales.Application.Dtos;
using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Domain.ValueObjects;
using BSX.Modules.Sales.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Sales.Infrastructure.Repositories;

/// <summary>Projection-based lead reads; list queries never load aggregates.</summary>
internal sealed class LeadReadRepository : ILeadReadRepository
{
    private const int DefaultSize = 20;
    private const int MaxSize = 100;

    private readonly SalesDbContext _context;

    public LeadReadRepository(SalesDbContext context) => _context = context;

    public async Task<LeadDto?> GetByIdAsync(
        Guid tenantId,
        Guid leadId,
        CancellationToken cancellationToken = default)
    {
        TenantId tenant = TenantId.From(tenantId);
        LeadId id = LeadId.From(leadId);

        var row = await _context.Leads
            .AsNoTracking()
            .Where(lead => lead.TenantId == tenant && lead.Id == id)
            .Select(lead => new
            {
                lead.Id,
                lead.TenantId,
                lead.Title,
                lead.Status,
                lead.CustomerId,
                lead.OrganizationId,
                lead.Notes,
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return row is null
            ? null
            : new LeadDto(
                row.Id.Value,
                row.TenantId.Value,
                row.Title.Value,
                row.Status.ToString(),
                row.CustomerId?.Value,
                row.OrganizationId?.Value,
                row.Notes);
    }

    public async Task<PagedResult<LeadSummaryDto>> ListAsync(
        Guid tenantId,
        int page,
        int size,
        string? search,
        CancellationToken cancellationToken = default)
    {
        int normalizedPage = page < 1 ? 1 : page;
        int normalizedSize = size is < 1 or > MaxSize ? DefaultSize : size;
        TenantId tenant = TenantId.From(tenantId);

        IQueryable<Lead> query = _context.Leads
            .AsNoTracking()
            .Where(lead => lead.TenantId == tenant);

        if (!string.IsNullOrWhiteSpace(search))
        {
            string pattern = $"%{search.Trim().ToLowerInvariant()}%";
            query = query.Where(lead =>
                EF.Functions.Like(EF.Property<string>(lead, "SearchTitle"), pattern));
        }

        int total = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var rows = await query
            .OrderBy(lead => EF.Property<string>(lead, "SearchTitle"))
            .ThenBy(lead => lead.Id)
            .Skip((normalizedPage - 1) * normalizedSize)
            .Take(normalizedSize)
            .Select(lead => new
            {
                lead.Id,
                lead.Title,
                lead.Status,
                lead.CustomerId,
                lead.OrganizationId,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        List<LeadSummaryDto> items = rows
            .Select(row => new LeadSummaryDto(
                row.Id.Value,
                row.Title.Value,
                row.Status.ToString(),
                row.CustomerId?.Value,
                row.OrganizationId?.Value))
            .ToList();

        return new PagedResult<LeadSummaryDto>(items, normalizedPage, normalizedSize, total);
    }
}
