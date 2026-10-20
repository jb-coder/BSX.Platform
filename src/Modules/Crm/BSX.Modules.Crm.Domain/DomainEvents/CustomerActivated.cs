using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Crm.Domain.DomainEvents;

/// <summary>Raised when a customer is (re)activated.</summary>
/// <param name="CustomerId">The customer identifier.</param>
public sealed record CustomerActivated(CustomerId CustomerId) : DomainEvent;
