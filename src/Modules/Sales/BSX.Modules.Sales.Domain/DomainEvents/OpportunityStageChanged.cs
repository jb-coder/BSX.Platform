using BSX.Modules.Sales.Domain.Enums;
using BSX.Modules.Sales.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Sales.Domain.DomainEvents;

/// <summary>Raised when an opportunity advances to the next sales stage.</summary>
/// <param name="OpportunityId">The opportunity identifier.</param>
/// <param name="From">The previous stage.</param>
/// <param name="To">The new stage.</param>
public sealed record OpportunityStageChanged(
    OpportunityId OpportunityId,
    OpportunityStage From,
    OpportunityStage To) : DomainEvent;
