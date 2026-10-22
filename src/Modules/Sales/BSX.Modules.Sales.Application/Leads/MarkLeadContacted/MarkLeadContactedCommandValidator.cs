using FluentValidation;

namespace BSX.Modules.Sales.Application.Leads.MarkLeadContacted;

/// <summary>Validates <see cref="MarkLeadContactedCommand"/>.</summary>
public sealed class MarkLeadContactedCommandValidator : AbstractValidator<MarkLeadContactedCommand>
{
    /// <summary>Initializes a new instance of the <see cref="MarkLeadContactedCommandValidator"/> class.</summary>
    public MarkLeadContactedCommandValidator() => RuleFor(command => command.LeadId).NotEmpty();
}
