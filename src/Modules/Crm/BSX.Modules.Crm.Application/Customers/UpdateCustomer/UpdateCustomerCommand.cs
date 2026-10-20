using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;

namespace BSX.Modules.Crm.Application.Customers.UpdateCustomer;

/// <summary>Updates a customer's descriptive fields.</summary>
/// <param name="CustomerId">The customer identifier.</param>
/// <param name="Name">The new name.</param>
/// <param name="TaxIdentifier">The new tax identifier, or <see langword="null"/> to clear it.</param>
[RequirePermission("crm.customers.update")]
public sealed record UpdateCustomerCommand(Guid CustomerId, string Name, string? TaxIdentifier) : ICommand;
