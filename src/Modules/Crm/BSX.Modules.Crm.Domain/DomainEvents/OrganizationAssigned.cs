using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Crm.Domain.DomainEvents;

/// <summary>Raised when a customer is assigned to an organization.</summary>
/// <param name="CustomerId">The customer identifier.</param>
/// <param name="OrganizationId">The organization identifier.</param>
public sealed record OrganizationAssigned(CustomerId CustomerId, OrganizationId OrganizationId) : DomainEvent;
