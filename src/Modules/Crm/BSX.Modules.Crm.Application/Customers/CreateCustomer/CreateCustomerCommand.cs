using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;

namespace BSX.Modules.Crm.Application.Customers.CreateCustomer;

/// <summary>Creates a customer in the active tenant.</summary>
/// <param name="Code">The unique customer code.</param>
/// <param name="Name">The customer name.</param>
/// <param name="TaxIdentifier">The optional tax identifier.</param>
[RequirePermission("crm.customers.create")]
public sealed record CreateCustomerCommand(string Code, string Name, string? TaxIdentifier) : ICommand<Guid>;
