using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Crm.Domain.DomainEvents;

/// <summary>Raised when a customer is created.</summary>
/// <param name="CustomerId">The customer identifier.</param>
/// <param name="CustomerCode">The customer code.</param>
public sealed record CustomerCreated(CustomerId CustomerId, CustomerCode CustomerCode) : DomainEvent;
