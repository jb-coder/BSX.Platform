using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Crm.Domain.DomainEvents;

/// <summary>Raised when a customer is deactivated.</summary>
/// <param name="CustomerId">The customer identifier.</param>
public sealed record CustomerDeactivated(CustomerId CustomerId) : DomainEvent;
