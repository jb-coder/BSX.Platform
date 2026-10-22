using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Sales.Application.Abstractions;
using BSX.Modules.Sales.Application.Errors;
using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Domain.Opportunities;
using BSX.Modules.Sales.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Sales.Application.Opportunities.CreateOpportunityFromLead;

/// <summary>Handles <see cref="CreateOpportunityFromLeadCommand"/>.</summary>
public sealed class CreateOpportunityFromLeadCommandHandler
    : ICommandHandler<CreateOpportunityFromLeadCommand, Guid>
{
    private readonly ILeadRepository _leads;
    private readonly IOpportunityRepository _opportunities;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SalesTenantResolver _tenantResolver;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="CreateOpportunityFromLeadCommandHandler"/> class.</summary>
    public CreateOpportunityFromLeadCommandHandler(
        ILeadRepository leads,
        IOpportunityRepository opportunities,
        IUnitOfWork unitOfWork,
        SalesTenantResolver tenantResolver,
        TimeProvider timeProvider)
    {
        _leads = leads;
        _opportunities = opportunities;
        _unitOfWork = unitOfWork;
        _tenantResolver = tenantResolver;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> HandleAsync(
        CreateOpportunityFromLeadCommand request,
        CancellationToken cancellationToken = default)
    {
        if (request.LeadId == Guid.Empty)
        {
            return Result.Failure<Guid>(SalesApplicationErrors.InvalidLeadId);
        }

        if (request.CustomerId == Guid.Empty)
        {
            return Result.Failure<Guid>(SalesApplicationErrors.InvalidCustomerId);
        }

        Result<TenantId> tenant = _tenantResolver.Resolve();
        if (tenant.IsFailure)
        {
            return Result.Failure<Guid>(tenant.Error);
        }

        Lead? lead = await _leads
            .GetByIdAsync(tenant.Value, LeadId.From(request.LeadId), cancellationToken)
            .ConfigureAwait(false);

        if (lead is null)
        {
            return Result.Failure<Guid>(SalesApplicationErrors.LeadNotFound);
        }

        if (await _opportunities
            .ExistsByLeadIdAsync(tenant.Value, lead.Id, cancellationToken)
            .ConfigureAwait(false))
        {
            return Result.Failure<Guid>(SalesApplicationErrors.OpportunityAlreadyExistsForLead);
        }

        Result<OpportunityTitle> title = OpportunityTitle.Create(request.Title);
        if (title.IsFailure)
        {
            return Result.Failure<Guid>(title.Error);
        }

        Result<Money> amount = Money.Create(request.Amount, request.Currency);
        if (amount.IsFailure)
        {
            return Result.Failure<Guid>(amount.Error);
        }

        Result<Probability> probability = Probability.Create(request.Probability);
        if (probability.IsFailure)
        {
            return Result.Failure<Guid>(probability.Error);
        }

        Result<ExpectedCloseDate> expectedCloseDate = ExpectedCloseDate.Create(
            request.ExpectedCloseDate,
            DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime));

        if (expectedCloseDate.IsFailure)
        {
            return Result.Failure<Guid>(expectedCloseDate.Error);
        }

        Result<Opportunity> created = Opportunity.CreateFromLead(
            OpportunityId.New(),
            tenant.Value,
            lead,
            CustomerId.From(request.CustomerId),
            title.Value,
            amount.Value,
            probability.Value,
            expectedCloseDate.Value);

        if (created.IsFailure)
        {
            return Result.Failure<Guid>(created.Error);
        }

        await _opportunities.AddAsync(created.Value, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(created.Value.Id.Value);
    }
}
