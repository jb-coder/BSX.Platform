using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Crm.Domain.DomainEvents;

/// <summary>Raised when a contact person is added to a customer.</summary>
/// <param name="CustomerId">The customer identifier.</param>
/// <param name="ContactPersonId">The contact person identifier.</param>
public sealed record ContactAdded(CustomerId CustomerId, ContactPersonId ContactPersonId) : DomainEvent;
