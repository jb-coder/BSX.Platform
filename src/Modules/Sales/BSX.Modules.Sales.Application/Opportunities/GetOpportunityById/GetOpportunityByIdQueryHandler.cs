using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Sales.Application.Abstractions;
using BSX.Modules.Sales.Application.Dtos;
using BSX.Modules.Sales.Application.Errors;
using BSX.Modules.Sales.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Sales.Application.Opportunities.GetOpportunityById;

/// <summary>Handles <see cref="GetOpportunityByIdQuery"/>. Projects through the read model; loads no aggregate.</summary>
public sealed class GetOpportunityByIdQueryHandler : IQueryHandler<GetOpportunityByIdQuery, OpportunityDto>
{
    private readonly IOpportunityReadRepository _opportunities;
    private readonly SalesTenantResolver _tenantResolver;

    /// <summary>Initializes a new instance of the <see cref="GetOpportunityByIdQueryHandler"/> class.</summary>
    public GetOpportunityByIdQueryHandler(
        IOpportunityReadRepository opportunities,
        SalesTenantResolver tenantResolver)
    {
        _opportunities = opportunities;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc />
    public async Task<Result<OpportunityDto>> HandleAsync(
        GetOpportunityByIdQuery request,
        CancellationToken cancellationToken = default)
    {
        if (request.OpportunityId == Guid.Empty)
        {
            return Result.Failure<OpportunityDto>(SalesApplicationErrors.InvalidOpportunityId);
        }

        Result<TenantId> tenant = _tenantResolver.Resolve();
        if (tenant.IsFailure)
        {
            return Result.Failure<OpportunityDto>(tenant.Error);
        }

        OpportunityDto? opportunity = await _opportunities
            .GetByIdAsync(tenant.Value.Value, request.OpportunityId, cancellationToken)
            .ConfigureAwait(false);

        return opportunity is null
            ? Result.Failure<OpportunityDto>(SalesApplicationErrors.OpportunityNotFound)
            : Result.Success(opportunity);
    }
}
