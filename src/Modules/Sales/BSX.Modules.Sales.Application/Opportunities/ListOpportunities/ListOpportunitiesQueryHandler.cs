using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Sales.Application.Abstractions;
using BSX.Modules.Sales.Application.Dtos;
using BSX.Modules.Sales.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Sales.Application.Opportunities.ListOpportunities;

/// <summary>
/// Handles <see cref="ListOpportunitiesQuery"/>. Projects through the read model; loads no aggregate.
/// </summary>
public sealed class ListOpportunitiesQueryHandler
    : IQueryHandler<ListOpportunitiesQuery, PagedResult<OpportunitySummaryDto>>
{
    private const int MaxSize = 100;
    private const int DefaultSize = 20;

    private readonly IOpportunityReadRepository _opportunities;
    private readonly SalesTenantResolver _tenantResolver;

    /// <summary>Initializes a new instance of the <see cref="ListOpportunitiesQueryHandler"/> class.</summary>
    public ListOpportunitiesQueryHandler(
        IOpportunityReadRepository opportunities,
        SalesTenantResolver tenantResolver)
    {
        _opportunities = opportunities;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<OpportunitySummaryDto>>> HandleAsync(
        ListOpportunitiesQuery request,
        CancellationToken cancellationToken = default)
    {
        Result<TenantId> tenant = _tenantResolver.Resolve();
        if (tenant.IsFailure)
        {
            return Result.Failure<PagedResult<OpportunitySummaryDto>>(tenant.Error);
        }

        int page = request.Page < 1 ? 1 : request.Page;
        int size = request.Size is < 1 or > MaxSize ? DefaultSize : request.Size;

        PagedResult<OpportunitySummaryDto> result = await _opportunities
            .ListAsync(tenant.Value.Value, page, size, request.Search, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(result);
    }
}
