using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;

namespace BSX.Modules.Crm.Application.Customers.DeactivateCustomer;

/// <summary>Deactivates a customer.</summary>
/// <param name="CustomerId">The customer identifier.</param>
[RequirePermission("crm.customers.update")]
public sealed record DeactivateCustomerCommand(Guid CustomerId) : ICommand;
