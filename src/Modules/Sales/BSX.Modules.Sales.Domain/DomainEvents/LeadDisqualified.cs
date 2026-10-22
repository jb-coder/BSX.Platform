using BSX.Modules.Sales.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Sales.Domain.DomainEvents;

/// <summary>Raised when a lead is disqualified.</summary>
/// <param name="LeadId">The lead identifier.</param>
public sealed record LeadDisqualified(LeadId LeadId) : DomainEvent;
