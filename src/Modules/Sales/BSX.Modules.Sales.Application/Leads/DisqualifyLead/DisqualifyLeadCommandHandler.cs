using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Sales.Application.Abstractions;
using BSX.Modules.Sales.Application.Errors;
using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Sales.Application.Leads.DisqualifyLead;

/// <summary>Handles <see cref="DisqualifyLeadCommand"/>.</summary>
public sealed class DisqualifyLeadCommandHandler : ICommandHandler<DisqualifyLeadCommand>
{
    private readonly ILeadRepository _leads;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SalesTenantResolver _tenantResolver;

    /// <summary>Initializes a new instance of the <see cref="DisqualifyLeadCommandHandler"/> class.</summary>
    public DisqualifyLeadCommandHandler(
        ILeadRepository leads,
        IUnitOfWork unitOfWork,
        SalesTenantResolver tenantResolver)
    {
        _leads = leads;
        _unitOfWork = unitOfWork;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc />
    public async Task<Result> HandleAsync(DisqualifyLeadCommand request, CancellationToken cancellationToken = default)
    {
        if (request.LeadId == Guid.Empty)
        {
            return Result.Failure(SalesApplicationErrors.InvalidLeadId);
        }

        Result<TenantId> tenant = _tenantResolver.Resolve();
        if (tenant.IsFailure)
        {
            return Result.Failure(tenant.Error);
        }

        Lead? lead = await _leads
            .GetByIdAsync(tenant.Value, LeadId.From(request.LeadId), cancellationToken)
            .ConfigureAwait(false);

        if (lead is null)
        {
            return Result.Failure(SalesApplicationErrors.LeadNotFound);
        }

        Result result = lead.Disqualify();
        if (result.IsFailure)
        {
            return result;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
