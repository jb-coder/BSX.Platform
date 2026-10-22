using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Domain.ValueObjects;
using FluentValidation;

namespace BSX.Modules.Sales.Application.Leads.UpdateLead;

/// <summary>Validates <see cref="UpdateLeadCommand"/> (shape only; the domain validates values).</summary>
public sealed class UpdateLeadCommandValidator : AbstractValidator<UpdateLeadCommand>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateLeadCommandValidator"/> class.</summary>
    public UpdateLeadCommandValidator()
    {
        RuleFor(command => command.LeadId).NotEmpty();
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
