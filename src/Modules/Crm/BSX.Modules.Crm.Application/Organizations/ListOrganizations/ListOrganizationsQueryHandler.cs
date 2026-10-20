using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Crm.Application.Abstractions;
using BSX.Modules.Crm.Application.Dtos;
using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Crm.Application.Organizations.ListOrganizations;

/// <summary>Handles <see cref="ListOrganizationsQuery"/>. Projects through the read model; loads no aggregate.</summary>
public sealed class ListOrganizationsQueryHandler : IQueryHandler<ListOrganizationsQuery, PagedResult<OrganizationDto>>
{
    private const int MaxSize = 100;
    private const int DefaultSize = 20;

    private readonly IOrganizationReadRepository _organizations;
    private readonly CrmTenantResolver _tenantResolver;

    /// <summary>Initializes a new instance of the <see cref="ListOrganizationsQueryHandler"/> class.</summary>
    public ListOrganizationsQueryHandler(
        IOrganizationReadRepository organizations,
        CrmTenantResolver tenantResolver)
    {
        _organizations = organizations;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<OrganizationDto>>> HandleAsync(
        ListOrganizationsQuery request,
        CancellationToken cancellationToken = default)
    {
        Result<TenantId> tenant = _tenantResolver.Resolve();
        if (tenant.IsFailure)
        {
            return Result.Failure<PagedResult<OrganizationDto>>(tenant.Error);
        }

        int page = request.Page < 1 ? 1 : request.Page;
        int size = request.Size is < 1 or > MaxSize ? DefaultSize : request.Size;

        PagedResult<OrganizationDto> result = await _organizations
            .ListAsync(tenant.Value.Value, page, size, request.Search, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(result);
    }
}
