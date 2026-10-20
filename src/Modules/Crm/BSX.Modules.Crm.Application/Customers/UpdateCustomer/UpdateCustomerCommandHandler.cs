using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Crm.Application.Abstractions;
using BSX.Modules.Crm.Application.Errors;
using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Crm.Application.Customers.UpdateCustomer;

/// <summary>Handles <see cref="UpdateCustomerCommand"/>.</summary>
public sealed class UpdateCustomerCommandHandler : ICommandHandler<UpdateCustomerCommand>
{
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CrmTenantResolver _tenantResolver;

    /// <summary>Initializes a new instance of the <see cref="UpdateCustomerCommandHandler"/> class.</summary>
    public UpdateCustomerCommandHandler(
        ICustomerRepository customers,
        IUnitOfWork unitOfWork,
        CrmTenantResolver tenantResolver)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc />
    public async Task<Result> HandleAsync(UpdateCustomerCommand request, CancellationToken cancellationToken = default)
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

        Result<CustomerName> name = CustomerName.Create(request.Name);
        if (name.IsFailure)
        {
            return Result.Failure(name.Error);
        }

        TaxIdentifier? taxIdentifier = null;
        if (!string.IsNullOrWhiteSpace(request.TaxIdentifier))
        {
            Result<TaxIdentifier> tax = TaxIdentifier.Create(request.TaxIdentifier);
            if (tax.IsFailure)
            {
                return Result.Failure(tax.Error);
            }

            taxIdentifier = tax.Value;
        }

        Customer? customer = await _customers
            .GetByIdAsync(tenant.Value, CustomerId.From(request.CustomerId), cancellationToken)
            .ConfigureAwait(false);

        if (customer is null)
        {
            return Result.Failure(CrmApplicationErrors.CustomerNotFound);
        }

        Result nameResult = customer.ChangeName(name.Value);
        if (nameResult.IsFailure)
        {
            return nameResult;
        }

        Result taxResult = customer.ChangeTaxIdentifier(taxIdentifier);
        if (taxResult.IsFailure)
        {
            return taxResult;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
