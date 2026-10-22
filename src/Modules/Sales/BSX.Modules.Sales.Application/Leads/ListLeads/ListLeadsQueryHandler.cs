using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Sales.Application.Abstractions;
using BSX.Modules.Sales.Application.Dtos;
using BSX.Modules.Sales.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Sales.Application.Leads.ListLeads;

/// <summary>Handles <see cref="ListLeadsQuery"/>. Projects through the read model; loads no aggregate.</summary>
public sealed class ListLeadsQueryHandler : IQueryHandler<ListLeadsQuery, PagedResult<LeadSummaryDto>>
{
    private const int MaxSize = 100;
    private const int DefaultSize = 20;

    private readonly ILeadReadRepository _leads;
    private readonly SalesTenantResolver _tenantResolver;

    /// <summary>Initializes a new instance of the <see cref="ListLeadsQueryHandler"/> class.</summary>
    public ListLeadsQueryHandler(ILeadReadRepository leads, SalesTenantResolver tenantResolver)
    {
        _leads = leads;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<LeadSummaryDto>>> HandleAsync(
        ListLeadsQuery request,
        CancellationToken cancellationToken = default)
    {
        Result<TenantId> tenant = _tenantResolver.Resolve();
        if (tenant.IsFailure)
        {
            return Result.Failure<PagedResult<LeadSummaryDto>>(tenant.Error);
        }

        int page = request.Page < 1 ? 1 : request.Page;
        int size = request.Size is < 1 or > MaxSize ? DefaultSize : request.Size;

        PagedResult<LeadSummaryDto> result = await _leads
            .ListAsync(tenant.Value.Value, page, size, request.Search, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(result);
    }
}
