using BSX.Modules.Sales.Domain.ValueObjects;
using FluentValidation;

namespace BSX.Modules.Sales.Application.Opportunities.CreateOpportunityFromLead;

/// <summary>
/// Validates <see cref="CreateOpportunityFromLeadCommand"/> (shape only; the domain validates
/// qualification, money, probability and date rules).
/// </summary>
public sealed class CreateOpportunityFromLeadCommandValidator : AbstractValidator<CreateOpportunityFromLeadCommand>
{
    /// <summary>Initializes a new instance of the <see cref="CreateOpportunityFromLeadCommandValidator"/> class.</summary>
    public CreateOpportunityFromLeadCommandValidator()
    {
        RuleFor(command => command.LeadId).NotEmpty();
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.Title).NotEmpty().MaximumLength(OpportunityTitle.MaxLength);
        RuleFor(command => command.Currency).NotEmpty().Length(3);
        RuleFor(command => command.ExpectedCloseDate).NotEqual(default(DateOnly));
    }
}
