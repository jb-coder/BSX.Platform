using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Crm.Application.Abstractions;
using BSX.Modules.Crm.Application.Errors;
using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Crm.Application.Contacts.UpdateContact;

/// <summary>Handles <see cref="UpdateContactCommand"/>.</summary>
public sealed class UpdateContactCommandHandler : ICommandHandler<UpdateContactCommand>
{
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CrmTenantResolver _tenantResolver;

    /// <summary>Initializes a new instance of the <see cref="UpdateContactCommandHandler"/> class.</summary>
    public UpdateContactCommandHandler(
        ICustomerRepository customers,
        IUnitOfWork unitOfWork,
        CrmTenantResolver tenantResolver)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc />
    public async Task<Result> HandleAsync(UpdateContactCommand request, CancellationToken cancellationToken = default)
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

        Result<CustomerName> name = CustomerName.Create(request.Name);
        if (name.IsFailure)
        {
            return Result.Failure(name.Error);
        }

        Email? email = null;
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            Result<Email> emailResult = Email.Create(request.Email);
            if (emailResult.IsFailure)
            {
                return Result.Failure(emailResult.Error);
            }

            email = emailResult.Value;
        }

        PhoneNumber? phoneNumber = null;
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            Result<PhoneNumber> phoneResult = PhoneNumber.Create(request.PhoneNumber);
            if (phoneResult.IsFailure)
            {
                return Result.Failure(phoneResult.Error);
            }

            phoneNumber = phoneResult.Value;
        }

        Customer? customer = await _customers
            .GetByIdAsync(tenant.Value, CustomerId.From(request.CustomerId), cancellationToken)
            .ConfigureAwait(false);

        if (customer is null)
        {
            return Result.Failure(CrmApplicationErrors.CustomerNotFound);
        }

        Result result = customer.ChangeContactDetails(
            ContactPersonId.From(request.ContactId),
            name.Value,
            email,
            phoneNumber);

        if (result.IsFailure)
        {
            return result;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
