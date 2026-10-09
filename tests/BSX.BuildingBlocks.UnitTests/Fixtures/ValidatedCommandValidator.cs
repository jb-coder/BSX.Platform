using FluentValidation;

namespace BSX.BuildingBlocks.UnitTests.Fixtures;

public sealed class ValidatedCommandValidator : AbstractValidator<ValidatedCommand>
{
    public ValidatedCommandValidator() => RuleFor(command => command.Name).NotEmpty();
}
