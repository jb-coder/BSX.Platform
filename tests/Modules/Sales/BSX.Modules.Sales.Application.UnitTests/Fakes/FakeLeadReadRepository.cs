using BSX.Modules.Sales.Application.Abstractions;
using BSX.Modules.Sales.Application.Dtos;

namespace BSX.Modules.Sales.Application.UnitTests.Fakes;

internal sealed class FakeLeadReadRepository : ILeadReadRepository
{
    public LeadDto? Lead { get; set; }

    public PagedResult<LeadSummaryDto> Page { get; set; } = new([], 1, 20, 0);

    public Guid? LastTenantId { get; private set; }

    public int LastPage { get; private set; }

    public int LastSize { get; private set; }

    public string? LastSearch { get; private set; }

    public Task<LeadDto?> GetByIdAsync(Guid tenantId, Guid leadId, CancellationToken cancellationToken = default)
    {
        LastTenantId = tenantId;
        return Task.FromResult(Lead is not null && Lead.Id == leadId ? Lead : null);
    }

    public Task<PagedResult<LeadSummaryDto>> ListAsync(
        Guid tenantId,
        int page,
        int size,
        string? search,
        CancellationToken cancellationToken = default)
    {
        LastTenantId = tenantId;
        LastPage = page;
        LastSize = size;
        LastSearch = search;
        return Task.FromResult(Page);
    }
}
