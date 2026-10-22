using FluentValidation;

namespace BSX.Modules.Sales.Application.Leads.QualifyLead;

/// <summary>Validates <see cref="QualifyLeadCommand"/>.</summary>
public sealed class QualifyLeadCommandValidator : AbstractValidator<QualifyLeadCommand>
{
    /// <summary>Initializes a new instance of the <see cref="QualifyLeadCommandValidator"/> class.</summary>
    public QualifyLeadCommandValidator() => RuleFor(command => command.LeadId).NotEmpty();
}
