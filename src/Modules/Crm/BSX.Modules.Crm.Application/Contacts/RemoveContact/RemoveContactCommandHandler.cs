using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Crm.Application.Abstractions;
using BSX.Modules.Crm.Application.Errors;
using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Crm.Application.Contacts.RemoveContact;

/// <summary>Handles <see cref="RemoveContactCommand"/>.</summary>
public sealed class RemoveContactCommandHandler : ICommandHandler<RemoveContactCommand>
{
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CrmTenantResolver _tenantResolver;

    /// <summary>Initializes a new instance of the <see cref="RemoveContactCommandHandler"/> class.</summary>
    public RemoveContactCommandHandler(
        ICustomerRepository customers,
        IUnitOfWork unitOfWork,
        CrmTenantResolver tenantResolver)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc />
    public async Task<Result> HandleAsync(RemoveContactCommand request, CancellationToken cancellationToken = default)
    {
        if (request.CustomerId == Guid.Empty)
        {
            return Result.Failure(CrmApplicationErrors.InvalidCustomerId);
        }

        if (request.ContactId == Guid.Empty)
        {
            return Result.Failure(CrmApplicationErrors.InvalidContactId);
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

        Result result = customer.RemoveContact(ContactPersonId.From(request.ContactId));
        if (result.IsFailure)
        {
            return result;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
