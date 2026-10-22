using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Sales.Application.Abstractions;
using BSX.Modules.Sales.Application.Errors;
using BSX.Modules.Sales.Domain.Opportunities;
using BSX.Modules.Sales.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Sales.Application.Opportunities.MarkOpportunityLost;

/// <summary>Handles <see cref="MarkOpportunityLostCommand"/>.</summary>
public sealed class MarkOpportunityLostCommandHandler : ICommandHandler<MarkOpportunityLostCommand>
{
    private readonly IOpportunityRepository _opportunities;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SalesTenantResolver _tenantResolver;

    /// <summary>Initializes a new instance of the <see cref="MarkOpportunityLostCommandHandler"/> class.</summary>
    public MarkOpportunityLostCommandHandler(
        IOpportunityRepository opportunities,
        IUnitOfWork unitOfWork,
        SalesTenantResolver tenantResolver)
    {
        _opportunities = opportunities;
        _unitOfWork = unitOfWork;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc />
    public async Task<Result> HandleAsync(MarkOpportunityLostCommand request, CancellationToken cancellationToken = default)
    {
        if (request.OpportunityId == Guid.Empty)
        {
            return Result.Failure(SalesApplicationErrors.InvalidOpportunityId);
        }

        Result<TenantId> tenant = _tenantResolver.Resolve();
        if (tenant.IsFailure)
        {
            return Result.Failure(tenant.Error);
        }

        Opportunity? opportunity = await _opportunities
            .GetByIdAsync(tenant.Value, OpportunityId.From(request.OpportunityId), cancellationToken)
            .ConfigureAwait(false);

        if (opportunity is null)
        {
            return Result.Failure(SalesApplicationErrors.OpportunityNotFound);
        }

        Result result = opportunity.MarkLost();
        if (result.IsFailure)
        {
            return result;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
