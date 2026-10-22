using FluentValidation;

namespace BSX.Modules.Sales.Application.Leads.DisqualifyLead;

/// <summary>Validates <see cref="DisqualifyLeadCommand"/>.</summary>
public sealed class DisqualifyLeadCommandValidator : AbstractValidator<DisqualifyLeadCommand>
{
    /// <summary>Initializes a new instance of the <see cref="DisqualifyLeadCommandValidator"/> class.</summary>
    public DisqualifyLeadCommandValidator() => RuleFor(command => command.LeadId).NotEmpty();
}
