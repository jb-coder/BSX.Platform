using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Crm.Application.Abstractions;
using BSX.Modules.Crm.Application.Dtos;
using BSX.Modules.Crm.Application.Errors;
using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Crm.Application.Customers.GetCustomerById;

/// <summary>Handles <see cref="GetCustomerByIdQuery"/>. Projects through the read model; loads no aggregate.</summary>
public sealed class GetCustomerByIdQueryHandler : IQueryHandler<GetCustomerByIdQuery, CustomerDto>
{
    private readonly ICustomerReadRepository _customers;
    private readonly CrmTenantResolver _tenantResolver;

    /// <summary>Initializes a new instance of the <see cref="GetCustomerByIdQueryHandler"/> class.</summary>
    public GetCustomerByIdQueryHandler(ICustomerReadRepository customers, CrmTenantResolver tenantResolver)
    {
        _customers = customers;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc />
    public async Task<Result<CustomerDto>> HandleAsync(GetCustomerByIdQuery request, CancellationToken cancellationToken = default)
    {
        if (request.CustomerId == Guid.Empty)
        {
            return Result.Failure<CustomerDto>(CrmApplicationErrors.InvalidCustomerId);
        }

        Result<TenantId> tenant = _tenantResolver.Resolve();
        if (tenant.IsFailure)
        {
            return Result.Failure<CustomerDto>(tenant.Error);
        }

        CustomerDto? customer = await _customers
            .GetByIdAsync(tenant.Value.Value, request.CustomerId, cancellationToken)
            .ConfigureAwait(false);

        return customer is null
            ? Result.Failure<CustomerDto>(CrmApplicationErrors.CustomerNotFound)
            : Result.Success(customer);
    }
}
