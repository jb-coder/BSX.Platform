using BSX.Modules.Crm.Application.Abstractions;
using BSX.Modules.Crm.Application.Dtos;

namespace BSX.Modules.Crm.Application.UnitTests.Fakes;

internal sealed class FakeOrganizationReadRepository : IOrganizationReadRepository
{
    public OrganizationDto? Organization { get; set; }

    public PagedResult<OrganizationDto> Page { get; set; } = new([], 1, 20, 0);

    public Guid? LastTenantId { get; private set; }

    public int LastPage { get; private set; }

    public int LastSize { get; private set; }

    public string? LastSearch { get; private set; }

    public Task<OrganizationDto?> GetByIdAsync(Guid tenantId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        LastTenantId = tenantId;
        return Task.FromResult(Organization is not null && Organization.Id == organizationId ? Organization : null);
    }

    public Task<PagedResult<OrganizationDto>> ListAsync(
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
