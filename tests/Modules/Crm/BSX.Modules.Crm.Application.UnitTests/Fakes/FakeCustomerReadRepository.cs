using BSX.Modules.Crm.Application.Abstractions;
using BSX.Modules.Crm.Application.Dtos;

namespace BSX.Modules.Crm.Application.UnitTests.Fakes;

internal sealed class FakeCustomerReadRepository : ICustomerReadRepository
{
    public CustomerDto? Customer { get; set; }

    public PagedResult<CustomerSummaryDto> Page { get; set; } = new([], 1, 20, 0);

    public Guid? LastTenantId { get; private set; }

    public int LastPage { get; private set; }

    public int LastSize { get; private set; }

    public string? LastSearch { get; private set; }

    public Task<CustomerDto?> GetByIdAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default)
    {
        LastTenantId = tenantId;
        return Task.FromResult(Customer is not null && Customer.Id == customerId ? Customer : null);
    }

    public Task<PagedResult<CustomerSummaryDto>> ListAsync(
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
