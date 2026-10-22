using BSX.Modules.Sales.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Sales.Domain.DomainEvents;

/// <summary>Raised when an opportunity is created from a qualified lead.</summary>
/// <param name="OpportunityId">The opportunity identifier.</param>
/// <param name="LeadId">The source lead identifier.</param>
/// <param name="Title">The opportunity title.</param>
public sealed record OpportunityCreated(OpportunityId OpportunityId, LeadId LeadId, OpportunityTitle Title) : DomainEvent;
