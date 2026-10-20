using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Crm.Application.Abstractions;
using BSX.Modules.Crm.Application.Dtos;
using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Crm.Application.Customers.ListCustomers;

/// <summary>Handles <see cref="ListCustomersQuery"/>. Projects through the read model; loads no aggregate.</summary>
public sealed class ListCustomersQueryHandler : IQueryHandler<ListCustomersQuery, PagedResult<CustomerSummaryDto>>
{
    private const int MaxSize = 100;
    private const int DefaultSize = 20;

    private readonly ICustomerReadRepository _customers;
    private readonly CrmTenantResolver _tenantResolver;

    /// <summary>Initializes a new instance of the <see cref="ListCustomersQueryHandler"/> class.</summary>
    public ListCustomersQueryHandler(ICustomerReadRepository customers, CrmTenantResolver tenantResolver)
    {
        _customers = customers;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<CustomerSummaryDto>>> HandleAsync(
        ListCustomersQuery request,
        CancellationToken cancellationToken = default)
    {
        Result<TenantId> tenant = _tenantResolver.Resolve();
        if (tenant.IsFailure)
        {
            return Result.Failure<PagedResult<CustomerSummaryDto>>(tenant.Error);
        }

        int page = request.Page < 1 ? 1 : request.Page;
        int size = request.Size is < 1 or > MaxSize ? DefaultSize : request.Size;

        PagedResult<CustomerSummaryDto> result = await _customers
            .ListAsync(tenant.Value.Value, page, size, request.Search, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(result);
    }
}
