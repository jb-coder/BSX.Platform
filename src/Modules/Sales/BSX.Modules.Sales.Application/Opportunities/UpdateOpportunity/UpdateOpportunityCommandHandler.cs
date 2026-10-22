using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Sales.Application.Abstractions;
using BSX.Modules.Sales.Application.Errors;
using BSX.Modules.Sales.Domain.Opportunities;
using BSX.Modules.Sales.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Sales.Application.Opportunities.UpdateOpportunity;

/// <summary>Handles <see cref="UpdateOpportunityCommand"/>.</summary>
public sealed class UpdateOpportunityCommandHandler : ICommandHandler<UpdateOpportunityCommand>
{
    private readonly IOpportunityRepository _opportunities;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SalesTenantResolver _tenantResolver;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="UpdateOpportunityCommandHandler"/> class.</summary>
    public UpdateOpportunityCommandHandler(
        IOpportunityRepository opportunities,
        IUnitOfWork unitOfWork,
        SalesTenantResolver tenantResolver,
        TimeProvider timeProvider)
    {
        _opportunities = opportunities;
        _unitOfWork = unitOfWork;
        _tenantResolver = tenantResolver;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<Result> HandleAsync(UpdateOpportunityCommand request, CancellationToken cancellationToken = default)
    {
        if (request.OpportunityId == Guid.Empty)
        {
            return Result.Failure(SalesApplicationErrors.InvalidOpportunityId);
        }

        if (request.CustomerId == Guid.Empty)
        {
            return Result.Failure(SalesApplicationErrors.InvalidCustomerId);
        }

        Result<TenantId> tenant = _tenantResolver.Resolve();
        if (tenant.IsFailure)
        {
            return Result.Failure(tenant.Error);
        }

        OrganizationId? organizationId = null;
        if (request.OrganizationId is { } organization)
        {
            if (organization == Guid.Empty)
            {
                return Result.Failure(SalesApplicationErrors.InvalidOrganizationId);
            }

            organizationId = OrganizationId.From(organization);
        }

        Result<OpportunityTitle> title = OpportunityTitle.Create(request.Title);
        if (title.IsFailure)
        {
            return Result.Failure(title.Error);
        }

        Result<Money> amount = Money.Create(request.Amount, request.Currency);
        if (amount.IsFailure)
        {
            return Result.Failure(amount.Error);
        }

        Result<Probability> probability = Probability.Create(request.Probability);
        if (probability.IsFailure)
        {
            return Result.Failure(probability.Error);
        }

        Result<ExpectedCloseDate> expectedCloseDate = ExpectedCloseDate.Create(
            request.ExpectedCloseDate,
            DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime));

        if (expectedCloseDate.IsFailure)
        {
            return Result.Failure(expectedCloseDate.Error);
        }

        Opportunity? opportunity = await _opportunities
            .GetByIdAsync(tenant.Value, OpportunityId.From(request.OpportunityId), cancellationToken)
            .ConfigureAwait(false);

        if (opportunity is null)
        {
            return Result.Failure(SalesApplicationErrors.OpportunityNotFound);
        }

        Result details = opportunity.UpdateDetails(
            title.Value,
            CustomerId.From(request.CustomerId),
            organizationId);

        if (details.IsFailure)
        {
            return details;
        }

        Result forecast = opportunity.UpdateForecast(
            amount.Value,
            probability.Value,
            expectedCloseDate.Value);

        if (forecast.IsFailure)
        {
            return forecast;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
