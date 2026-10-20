using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;

namespace BSX.Modules.Crm.Application.Contacts.SetPrimaryContact;

/// <summary>Marks a contact person as the customer's primary contact.</summary>
/// <param name="CustomerId">The customer identifier.</param>
/// <param name="ContactId">The contact person identifier.</param>
[RequirePermission("crm.customers.update")]
public sealed record SetPrimaryContactCommand(Guid CustomerId, Guid ContactId) : ICommand;
