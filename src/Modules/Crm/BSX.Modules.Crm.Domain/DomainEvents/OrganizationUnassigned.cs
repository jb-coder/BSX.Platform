using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Crm.Domain.DomainEvents;

/// <summary>Raised when a customer is detached from its organization.</summary>
/// <param name="CustomerId">The customer identifier.</param>
public sealed record OrganizationUnassigned(CustomerId CustomerId) : DomainEvent;
