using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Crm.Application.Abstractions;
using BSX.Modules.Crm.Application.Errors;
using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Crm.Application.Addresses.SetDefaultAddress;

/// <summary>Handles <see cref="SetDefaultAddressCommand"/>.</summary>
public sealed class SetDefaultAddressCommandHandler : ICommandHandler<SetDefaultAddressCommand>
{
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CrmTenantResolver _tenantResolver;

    /// <summary>Initializes a new instance of the <see cref="SetDefaultAddressCommandHandler"/> class.</summary>
    public SetDefaultAddressCommandHandler(
        ICustomerRepository customers,
        IUnitOfWork unitOfWork,
        CrmTenantResolver tenantResolver)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc />
    public async Task<Result> HandleAsync(SetDefaultAddressCommand request, CancellationToken cancellationToken = default)
    {
        if (request.CustomerId == Guid.Empty)
        {
            return Result.Failure(CrmApplicationErrors.InvalidCustomerId);
        }

        if (request.AddressId == Guid.Empty)
        {
            return Result.Failure(CrmApplicationErrors.InvalidAddressId);
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

        Result result = customer.SetDefaultAddress(CustomerAddressId.From(request.AddressId));
        if (result.IsFailure)
        {
            return result;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
