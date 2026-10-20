using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Crm.Domain.DomainEvents;

/// <summary>Raised when a contact person is removed from a customer.</summary>
/// <param name="CustomerId">The customer identifier.</param>
/// <param name="ContactPersonId">The contact person identifier.</param>
public sealed record ContactRemoved(CustomerId CustomerId, ContactPersonId ContactPersonId) : DomainEvent;
