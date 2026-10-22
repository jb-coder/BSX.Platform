using BSX.Modules.Sales.Application.Abstractions;
using BSX.Modules.Sales.Application.Dtos;

namespace BSX.Modules.Sales.Application.UnitTests.Fakes;

internal sealed class FakeOpportunityReadRepository : IOpportunityReadRepository
{
    public OpportunityDto? Opportunity { get; set; }

    public PagedResult<OpportunitySummaryDto> Page { get; set; } = new([], 1, 20, 0);

    public Guid? LastTenantId { get; private set; }

    public int LastPage { get; private set; }

    public int LastSize { get; private set; }

    public string? LastSearch { get; private set; }

    public Task<OpportunityDto?> GetByIdAsync(
        Guid tenantId,
        Guid opportunityId,
        CancellationToken cancellationToken = default)
    {
        LastTenantId = tenantId;
        return Task.FromResult(
            Opportunity is not null && Opportunity.Id == opportunityId ? Opportunity : null);
    }

    public Task<PagedResult<OpportunitySummaryDto>> ListAsync(
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
