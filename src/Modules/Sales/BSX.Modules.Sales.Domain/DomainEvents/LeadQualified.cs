using BSX.Modules.Sales.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Sales.Domain.DomainEvents;

/// <summary>Raised when a lead is qualified.</summary>
/// <param name="LeadId">The lead identifier.</param>
public sealed record LeadQualified(LeadId LeadId) : DomainEvent;
