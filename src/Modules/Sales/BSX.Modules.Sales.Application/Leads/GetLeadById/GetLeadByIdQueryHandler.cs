using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Sales.Application.Abstractions;
using BSX.Modules.Sales.Application.Dtos;
using BSX.Modules.Sales.Application.Errors;
using BSX.Modules.Sales.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Sales.Application.Leads.GetLeadById;

/// <summary>Handles <see cref="GetLeadByIdQuery"/>. Projects through the read model; loads no aggregate.</summary>
public sealed class GetLeadByIdQueryHandler : IQueryHandler<GetLeadByIdQuery, LeadDto>
{
    private readonly ILeadReadRepository _leads;
    private readonly SalesTenantResolver _tenantResolver;

    /// <summary>Initializes a new instance of the <see cref="GetLeadByIdQueryHandler"/> class.</summary>
    public GetLeadByIdQueryHandler(ILeadReadRepository leads, SalesTenantResolver tenantResolver)
    {
        _leads = leads;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc />
    public async Task<Result<LeadDto>> HandleAsync(GetLeadByIdQuery request, CancellationToken cancellationToken = default)
    {
        if (request.LeadId == Guid.Empty)
        {
            return Result.Failure<LeadDto>(SalesApplicationErrors.InvalidLeadId);
        }

        Result<TenantId> tenant = _tenantResolver.Resolve();
        if (tenant.IsFailure)
        {
            return Result.Failure<LeadDto>(tenant.Error);
        }

        LeadDto? lead = await _leads
            .GetByIdAsync(tenant.Value.Value, request.LeadId, cancellationToken)
            .ConfigureAwait(false);

        return lead is null
            ? Result.Failure<LeadDto>(SalesApplicationErrors.LeadNotFound)
            : Result.Success(lead);
    }
}
