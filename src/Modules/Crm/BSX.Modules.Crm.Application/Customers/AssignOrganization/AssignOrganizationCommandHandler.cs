using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Crm.Application.Abstractions;
using BSX.Modules.Crm.Application.Errors;
using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Domain.Organizations;
using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Crm.Application.Customers.AssignOrganization;

/// <summary>Handles <see cref="AssignOrganizationCommand"/>.</summary>
public sealed class AssignOrganizationCommandHandler : ICommandHandler<AssignOrganizationCommand>
{
    private readonly ICustomerRepository _customers;
    private readonly IOrganizationRepository _organizations;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CrmTenantResolver _tenantResolver;

    /// <summary>Initializes a new instance of the <see cref="AssignOrganizationCommandHandler"/> class.</summary>
    public AssignOrganizationCommandHandler(
        ICustomerRepository customers,
        IOrganizationRepository organizations,
        IUnitOfWork unitOfWork,
        CrmTenantResolver tenantResolver)
    {
        _customers = customers;
        _organizations = organizations;
        _unitOfWork = unitOfWork;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc />
    public async Task<Result> HandleAsync(AssignOrganizationCommand request, CancellationToken cancellationToken = default)
    {
        if (request.CustomerId == Guid.Empty)
        {
            return Result.Failure(CrmApplicationErrors.InvalidCustomerId);
        }

        if (request.OrganizationId == Guid.Empty)
        {
            return Result.Failure(CrmApplicationErrors.InvalidOrganizationId);
        }

        Result<TenantId> tenant = _tenantResolver.Resolve();
        if (tenant.IsFailure)
        {
            return Result.Failure(tenant.Error);
        }

        Customer? customer = await _customers
            .GetByIdAsync(tenant.Value, CustomerId.From(request.CustomerId), cancellationToken)
            .ConfigureAwait(false);

        if (customer is null)
        {
            return Result.Failure(CrmApplicationErrors.CustomerNotFound);
        }

        Organization? organization = await _organizations
            .GetByIdAsync(tenant.Value, OrganizationId.From(request.OrganizationId), cancellationToken)
            .ConfigureAwait(false);

        if (organization is null)
        {
            return Result.Failure(CrmApplicationErrors.OrganizationNotFound);
        }

        Result result = customer.AssignOrganization(organization.Id);
        if (result.IsFailure)
        {
            return result;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
