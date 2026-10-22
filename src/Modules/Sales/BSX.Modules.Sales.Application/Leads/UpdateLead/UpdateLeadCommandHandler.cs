using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Sales.Application.Abstractions;
using BSX.Modules.Sales.Application.Errors;
using BSX.Modules.Sales.Domain.Errors;
using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Sales.Application.Leads.UpdateLead;

/// <summary>Handles <see cref="UpdateLeadCommand"/>.</summary>
public sealed class UpdateLeadCommandHandler : ICommandHandler<UpdateLeadCommand>
{
    private readonly ILeadRepository _leads;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SalesTenantResolver _tenantResolver;

    /// <summary>Initializes a new instance of the <see cref="UpdateLeadCommandHandler"/> class.</summary>
    public UpdateLeadCommandHandler(
        ILeadRepository leads,
        IUnitOfWork unitOfWork,
        SalesTenantResolver tenantResolver)
    {
        _leads = leads;
        _unitOfWork = unitOfWork;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc />
    public async Task<Result> HandleAsync(UpdateLeadCommand request, CancellationToken cancellationToken = default)
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

        Result<LeadTitle> title = LeadTitle.Create(request.Title);
        if (title.IsFailure)
        {
            return Result.Failure(title.Error);
        }

        CustomerId? customerId = null;
        if (request.CustomerId is { } customer)
        {
            if (customer == Guid.Empty)
            {
                return Result.Failure(SalesApplicationErrors.InvalidCustomerId);
            }

            customerId = CustomerId.From(customer);
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

        Lead? lead = await _leads
            .GetByIdAsync(tenant.Value, LeadId.From(request.LeadId), cancellationToken)
            .ConfigureAwait(false);

        if (lead is null)
        {
            return Result.Failure(SalesApplicationErrors.LeadNotFound);
        }

        bool detailsChanged = !Equals(lead.Title, title.Value)
            || !Equals(lead.CustomerId, customerId)
            || !Equals(lead.OrganizationId, organizationId);

        if (lead.IsEditable)
        {
            Result details = lead.UpdateDetails(title.Value, customerId, organizationId);
            if (details.IsFailure)
            {
                return details;
            }
        }
        else if (detailsChanged)
        {
            return Result.Failure(SalesErrors.LeadNotEditable);
        }

        Result notes = lead.ChangeNotes(request.Notes);
        if (notes.IsFailure)
        {
            return notes;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
