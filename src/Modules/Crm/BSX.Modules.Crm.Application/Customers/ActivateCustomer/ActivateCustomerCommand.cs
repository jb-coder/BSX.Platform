using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;

namespace BSX.Modules.Crm.Application.Customers.ActivateCustomer;

/// <summary>Activates a customer.</summary>
/// <param name="CustomerId">The customer identifier.</param>
[RequirePermission("crm.customers.update")]
public sealed record ActivateCustomerCommand(Guid CustomerId) : ICommand;
