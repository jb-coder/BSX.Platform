using BSX.Modules.Sales.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Sales.Domain.DomainEvents;

/// <summary>Raised when a lead is created.</summary>
/// <param name="LeadId">The lead identifier.</param>
/// <param name="Title">The lead title.</param>
public sealed record LeadCreated(LeadId LeadId, LeadTitle Title) : DomainEvent;
