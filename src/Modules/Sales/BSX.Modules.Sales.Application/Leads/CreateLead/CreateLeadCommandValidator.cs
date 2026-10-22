using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Domain.ValueObjects;
using FluentValidation;

namespace BSX.Modules.Sales.Application.Leads.CreateLead;

/// <summary>Validates <see cref="CreateLeadCommand"/> (shape only; the domain validates values).</summary>
public sealed class CreateLeadCommandValidator : AbstractValidator<CreateLeadCommand>
{
    /// <summary>Initializes a new instance of the <see cref="CreateLeadCommandValidator"/> class.</summary>
    public CreateLeadCommandValidator()
    {
        RuleFor(command => command.Title).NotEmpty().MaximumLength(LeadTitle.MaxLength);
        RuleFor(command => command.Notes).MaximumLength(Lead.MaxNotesLength);
        RuleFor(command => command.CustomerId)
            .NotEqual(Guid.Empty)
            .When(command => command.CustomerId.HasValue);
        RuleFor(command => command.OrganizationId)
            .NotEqual(Guid.Empty)
            .When(command => command.OrganizationId.HasValue);
    }
}
