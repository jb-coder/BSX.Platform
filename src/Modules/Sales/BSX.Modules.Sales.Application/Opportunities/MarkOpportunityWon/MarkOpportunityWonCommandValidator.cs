using FluentValidation;

namespace BSX.Modules.Sales.Application.Opportunities.MarkOpportunityWon;

/// <summary>Validates <see cref="MarkOpportunityWonCommand"/>.</summary>
public sealed class MarkOpportunityWonCommandValidator : AbstractValidator<MarkOpportunityWonCommand>
{
    /// <summary>Initializes a new instance of the <see cref="MarkOpportunityWonCommandValidator"/> class.</summary>
    public MarkOpportunityWonCommandValidator() => RuleFor(command => command.OpportunityId).NotEmpty();
}
