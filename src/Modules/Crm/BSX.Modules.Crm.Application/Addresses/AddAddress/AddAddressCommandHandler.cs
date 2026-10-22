using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Crm.Application.Abstractions;
using BSX.Modules.Crm.Application.Common;
using BSX.Modules.Crm.Application.Errors;
using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Domain.Enums;
using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Crm.Application.Addresses.AddAddress;

/// <summary>Handles <see cref="AddAddressCommand"/>.</summary>
public sealed class AddAddressCommandHandler : ICommandHandler<AddAddressCommand>
{
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CrmTenantResolver _tenantResolver;

    /// <summary>Initializes a new instance of the <see cref="AddAddressCommandHandler"/> class.</summary>
    public AddAddressCommandHandler(
        ICustomerRepository customers,
        IUnitOfWork unitOfWork,
        CrmTenantResolver tenantResolver)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc />
    public async Task<Result> HandleAsync(AddAddressCommand request, CancellationToken cancellationToken = default)
    {
        if (request.CustomerId == Guid.Empty)
        {
            return Result.Failure(CrmApplicationErrors.InvalidCustomerId);
        }

        Result<TenantId> tenant = _tenantResolver.Resolve();
        if (tenant.IsFailure)
        {
            return Result.Failure(tenant.Error);
        }

        Result<Address> address = Address.Create(
            request.Address.Line1,
            request.Address.Line2,
            request.Address.City,
            request.Address.Region,
            request.Address.PostalCode,
            request.Address.Country);

        if (address.IsFailure)
        {
            return Result.Failure(address.Error);
        }

        Customer? customer = await _customers
            .GetByIdAsync(tenant.Value, CustomerId.From(request.CustomerId), cancellationToken)
            .ConfigureAwait(false);

        if (customer is null)
        {
            return Result.Failure(CrmApplicationErrors.CustomerNotFound);
        }

        Result result = customer.AddAddress(CustomerAddressId.New(), address.Value, ToDomain(request.Type));
        if (result.IsFailure)
        {
            return result;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private static AddressType ToDomain(CrmAddressType type) => type switch
    {
        CrmAddressType.Billing => AddressType.Billing,
        CrmAddressType.Shipping => AddressType.Shipping,
        _ => AddressType.Other,
    };
}
