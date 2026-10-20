using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;

namespace BSX.Modules.Crm.Application.Contacts.RemoveContact;

/// <summary>Removes a contact person from a customer.</summary>
/// <param name="CustomerId">The customer identifier.</param>
/// <param name="ContactId">The contact person identifier.</param>
[RequirePermission("crm.customers.update")]
public sealed record RemoveContactCommand(Guid CustomerId, Guid ContactId) : ICommand;
