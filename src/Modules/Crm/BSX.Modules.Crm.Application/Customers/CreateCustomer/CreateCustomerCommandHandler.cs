using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Crm.Application.Abstractions;
using BSX.Modules.Crm.Application.Errors;
using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Crm.Application.Customers.CreateCustomer;

/// <summary>Handles <see cref="CreateCustomerCommand"/>.</summary>
public sealed class CreateCustomerCommandHandler : ICommandHandler<CreateCustomerCommand, Guid>
{
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CrmTenantResolver _tenantResolver;

    /// <summary>Initializes a new instance of the <see cref="CreateCustomerCommandHandler"/> class.</summary>
    public CreateCustomerCommandHandler(
        ICustomerRepository customers,
        IUnitOfWork unitOfWork,
        CrmTenantResolver tenantResolver)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> HandleAsync(CreateCustomerCommand request, CancellationToken cancellationToken = default)
    {
        Result<TenantId> tenant = _tenantResolver.Resolve();
        if (tenant.IsFailure)
        {
            return Result.Failure<Guid>(tenant.Error);
        }

        Result<CustomerCode> code = CustomerCode.Create(request.Code);
        if (code.IsFailure)
        {
            return Result.Failure<Guid>(code.Error);
        }

        Result<CustomerName> name = CustomerName.Create(request.Name);
        if (name.IsFailure)
        {
            return Result.Failure<Guid>(name.Error);
        }

        TaxIdentifier? taxIdentifier = null;
        if (!string.IsNullOrWhiteSpace(request.TaxIdentifier))
        {
            Result<TaxIdentifier> tax = TaxIdentifier.Create(request.TaxIdentifier);
            if (tax.IsFailure)
            {
                return Result.Failure<Guid>(tax.Error);
            }

            taxIdentifier = tax.Value;
        }

        if (await _customers.ExistsByCodeAsync(tenant.Value, code.Value, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure<Guid>(CrmApplicationErrors.CustomerCodeAlreadyExists);
        }

        Result<Customer> created = Customer.Create(
            CustomerId.New(),
            tenant.Value,
            code.Value,
            name.Value,
            taxIdentifier);

        if (created.IsFailure)
        {
            return Result.Failure<Guid>(created.Error);
        }

        await _customers.AddAsync(created.Value, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(created.Value.Id.Value);
    }
}
