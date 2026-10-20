using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;

namespace BSX.Modules.Crm.Application.Contacts.UpdateContact;

/// <summary>Updates a contact person's details.</summary>
/// <param name="CustomerId">The customer identifier.</param>
/// <param name="ContactId">The contact person identifier.</param>
/// <param name="Name">The new name.</param>
/// <param name="Email">The new email, if any.</param>
/// <param name="PhoneNumber">The new phone number, if any.</param>
[RequirePermission("crm.customers.update")]
public sealed record UpdateContactCommand(
    Guid CustomerId,
    Guid ContactId,
    string Name,
    string? Email,
    string? PhoneNumber) : ICommand;
