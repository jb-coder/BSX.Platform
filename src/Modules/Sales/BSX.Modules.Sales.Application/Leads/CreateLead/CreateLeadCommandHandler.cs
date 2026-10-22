using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Sales.Application.Abstractions;
using BSX.Modules.Sales.Application.Errors;
using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Sales.Application.Leads.CreateLead;

/// <summary>Handles <see cref="CreateLeadCommand"/>.</summary>
public sealed class CreateLeadCommandHandler : ICommandHandler<CreateLeadCommand, Guid>
{
    private readonly ILeadRepository _leads;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SalesTenantResolver _tenantResolver;

    /// <summary>Initializes a new instance of the <see cref="CreateLeadCommandHandler"/> class.</summary>
    public CreateLeadCommandHandler(
        ILeadRepository leads,
        IUnitOfWork unitOfWork,
        SalesTenantResolver tenantResolver)
    {
        _leads = leads;
        _unitOfWork = unitOfWork;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> HandleAsync(CreateLeadCommand request, CancellationToken cancellationToken = default)
    {
        Result<TenantId> tenant = _tenantResolver.Resolve();
        if (tenant.IsFailure)
        {
            return Result.Failure<Guid>(tenant.Error);
        }

        Result<LeadTitle> title = LeadTitle.Create(request.Title);
        if (title.IsFailure)
        {
            return Result.Failure<Guid>(title.Error);
        }

        CustomerId? customerId = null;
        if (request.CustomerId is { } customer)
        {
            if (customer == Guid.Empty)
            {
                return Result.Failure<Guid>(SalesApplicationErrors.InvalidCustomerId);
            }

            customerId = CustomerId.From(customer);
        }

        OrganizationId? organizationId = null;
        if (request.OrganizationId is { } organization)
        {
            if (organization == Guid.Empty)
            {
                return Result.Failure<Guid>(SalesApplicationErrors.InvalidOrganizationId);
            }

            organizationId = OrganizationId.From(organization);
        }

        Result<Lead> created = Lead.Create(
            LeadId.New(),
            tenant.Value,
            title.Value,
            customerId,
            organizationId,
            request.Notes);

        if (created.IsFailure)
        {
            return Result.Failure<Guid>(created.Error);
        }

        await _leads.AddAsync(created.Value, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(created.Value.Id.Value);
    }
}
