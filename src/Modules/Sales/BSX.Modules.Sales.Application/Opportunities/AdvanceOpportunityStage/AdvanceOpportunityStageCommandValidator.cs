using FluentValidation;

namespace BSX.Modules.Sales.Application.Opportunities.AdvanceOpportunityStage;

/// <summary>Validates <see cref="AdvanceOpportunityStageCommand"/>.</summary>
public sealed class AdvanceOpportunityStageCommandValidator : AbstractValidator<AdvanceOpportunityStageCommand>
{
    /// <summary>Initializes a new instance of the <see cref="AdvanceOpportunityStageCommandValidator"/> class.</summary>
    public AdvanceOpportunityStageCommandValidator() => RuleFor(command => command.OpportunityId).NotEmpty();
}
