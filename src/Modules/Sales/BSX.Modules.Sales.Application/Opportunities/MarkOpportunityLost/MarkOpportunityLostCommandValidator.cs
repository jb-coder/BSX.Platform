using FluentValidation;

namespace BSX.Modules.Sales.Application.Opportunities.MarkOpportunityLost;

/// <summary>Validates <see cref="MarkOpportunityLostCommand"/>.</summary>
public sealed class MarkOpportunityLostCommandValidator : AbstractValidator<MarkOpportunityLostCommand>
{
    /// <summary>Initializes a new instance of the <see cref="MarkOpportunityLostCommandValidator"/> class.</summary>
    public MarkOpportunityLostCommandValidator() => RuleFor(command => command.OpportunityId).NotEmpty();
}
