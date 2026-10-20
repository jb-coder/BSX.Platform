using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;

namespace BSX.Modules.Crm.Application.Customers.AssignOrganization;

/// <summary>Assigns a customer to an organization in the active tenant.</summary>
/// <param name="CustomerId">The customer identifier.</param>
/// <param name="OrganizationId">The organization identifier.</param>
[RequirePermission("crm.customers.update")]
public sealed record AssignOrganizationCommand(Guid CustomerId, Guid OrganizationId) : ICommand;
