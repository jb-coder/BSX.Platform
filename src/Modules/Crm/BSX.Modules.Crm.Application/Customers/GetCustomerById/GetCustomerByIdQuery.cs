using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Crm.Application.Dtos;

namespace BSX.Modules.Crm.Application.Customers.GetCustomerById;

/// <summary>Gets a customer projection by identifier within the active tenant.</summary>
/// <param name="CustomerId">The customer identifier.</param>
[RequirePermission("crm.customers.read")]
public sealed record GetCustomerByIdQuery(Guid CustomerId) : IQuery<CustomerDto>;
