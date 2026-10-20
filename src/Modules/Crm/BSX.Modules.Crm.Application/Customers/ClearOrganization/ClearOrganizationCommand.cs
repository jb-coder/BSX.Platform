using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;

namespace BSX.Modules.Crm.Application.Customers.ClearOrganization;

/// <summary>Detaches a customer from its organization.</summary>
/// <param name="CustomerId">The customer identifier.</param>
[RequirePermission("crm.customers.update")]
public sealed record ClearOrganizationCommand(Guid CustomerId) : ICommand;
