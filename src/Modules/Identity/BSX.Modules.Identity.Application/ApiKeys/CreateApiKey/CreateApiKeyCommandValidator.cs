using FluentValidation;

namespace BSX.Modules.Identity.Application.ApiKeys.CreateApiKey;

/// <summary>Validates <see cref="CreateApiKeyCommand"/>.</summary>
public sealed class CreateApiKeyCommandValidator : AbstractValidator<CreateApiKeyCommand>
{
    /// <summary>Initializes a new instance of the <see cref="CreateApiKeyCommandValidator"/> class.</summary>
    public CreateApiKeyCommandValidator()
    {
        RuleFor(command => command.ServiceAccountId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.ExpiresOnUtc)
            .GreaterThan(DateTimeOffset.UtcNow)
            .When(command => command.ExpiresOnUtc.HasValue);
        RuleFor(command => command.Scopes)
            .Must(scopes => scopes is null || scopes.All(scope => !string.IsNullOrWhiteSpace(scope)))
            .WithMessage("Scopes must not contain empty values.");
    }
}
