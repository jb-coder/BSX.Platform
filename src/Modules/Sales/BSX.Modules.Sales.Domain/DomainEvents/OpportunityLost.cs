using BSX.Modules.Sales.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Sales.Domain.DomainEvents;

/// <summary>Raised when an opportunity is lost.</summary>
/// <param name="OpportunityId">The opportunity identifier.</param>
public sealed record OpportunityLost(OpportunityId OpportunityId) : DomainEvent;
