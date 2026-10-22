using BSX.Modules.Sales.Domain.ValueObjects;
using FluentValidation;

namespace BSX.Modules.Sales.Application.Opportunities.UpdateOpportunity;

/// <summary>
/// Validates <see cref="UpdateOpportunityCommand"/> (shape only; the domain validates money,
/// probability and date rules).
/// </summary>
public sealed class UpdateOpportunityCommandValidator : AbstractValidator<UpdateOpportunityCommand>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateOpportunityCommandValidator"/> class.</summary>
    public UpdateOpportunityCommandValidator()
    {
        RuleFor(command => command.OpportunityId).NotEmpty();
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.OrganizationId)
            .NotEqual(Guid.Empty)
            .When(command => command.OrganizationId.HasValue);
        RuleFor(command => command.Title).NotEmpty().MaximumLength(OpportunityTitle.MaxLength);
        RuleFor(command => command.Currency).NotEmpty().Length(3);
        RuleFor(command => command.ExpectedCloseDate).NotEqual(default(DateOnly));
    }
}
