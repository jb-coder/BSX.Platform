using System.Reflection;
using BSX.SharedKernel.Results;
using FluentValidation;
using FluentValidation.Results;

namespace BSX.BuildingBlocks.Behaviors;

/// <summary>
/// Runs all FluentValidation validators registered for the request before it reaches the handler.
/// Validation failures short-circuit the pipeline and are returned as a failed <see cref="Result"/>.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
internal sealed class ValidationBehavior<TRequest, TResponse> : Cqrs.IPipelineBehavior<TRequest, TResponse>
    where TRequest : Cqrs.IRequest<TResponse>
{
    private const string ValidationErrorCode = "Validation.Failed";

    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="validators">The validators registered for the request type.</param>
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    /// <inheritdoc />
    public async Task<TResponse> HandleAsync(
        TRequest request,
        Cqrs.RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(next);

        if (!_validators.Any())
        {
            return await next().ConfigureAwait(false);
        }

        var context = new ValidationContext<TRequest>(request);

        ValidationResult[] results = await Task
            .WhenAll(_validators.Select(validator => validator.ValidateAsync(context, cancellationToken)))
            .ConfigureAwait(false);

        ValidationFailure[] failures = results
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToArray();

        if (failures.Length == 0)
        {
            return await next().ConfigureAwait(false);
        }

        return CreateValidationResult(failures);
    }

    private static TResponse CreateValidationResult(IReadOnlyCollection<ValidationFailure> failures)
    {
        string message = string.Join("; ", failures.Select(failure => failure.ErrorMessage));
        Error error = Error.Validation(ValidationErrorCode, message);

        Type responseType = typeof(TResponse);

        if (responseType == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error);
        }

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            Type valueType = responseType.GetGenericArguments()[0];
            MethodInfo failureMethod = typeof(Result<>)
                .MakeGenericType(valueType)
                .GetMethod(nameof(Result<object>.Failure))!;

            return (TResponse)failureMethod.Invoke(null, [error])!;
        }

        throw new InvalidOperationException(
            $"The validation behavior requires requests to return '{typeof(Result)}' or '{typeof(Result<>)}'.");
    }
}
