using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;

namespace BSX.Modules.Crm.Application.Contacts.AddContact;

/// <summary>Adds a contact person to a customer.</summary>
/// <param name="CustomerId">The customer identifier.</param>
/// <param name="Name">The contact person's name.</param>
/// <param name="Email">The optional email address.</param>
/// <param name="PhoneNumber">The optional phone number.</param>
/// <param name="IsPrimary">Whether the contact should become primary.</param>
[RequirePermission("crm.customers.update")]
public sealed record AddContactCommand(
    Guid CustomerId,
    string Name,
    string? Email,
    string? PhoneNumber,
    bool IsPrimary) : ICommand;
