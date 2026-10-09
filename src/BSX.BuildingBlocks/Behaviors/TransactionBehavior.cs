using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.SharedKernel.Results;

namespace BSX.BuildingBlocks.Behaviors;

/// <summary>
/// Wraps commands in a transaction and triggers the post-commit persistence and domain event
/// dispatch seam (ADR-009). Queries bypass the behavior and never open a transaction.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
internal sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Initializes a new instance of the <see cref="TransactionBehavior{TRequest, TResponse}"/> class.</summary>
    /// <param name="unitOfWork">The unit of work that owns the transaction and the dispatch seam.</param>
    public TransactionBehavior(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <inheritdoc />
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(next);

        if (!CommandDetector.IsCommand(typeof(TRequest)))
        {
            return await next().ConfigureAwait(false);
        }

        await using IDatabaseTransaction transaction = await _unitOfWork
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        TResponse response = await next().ConfigureAwait(false);

        if (response is Result result && result.IsFailure)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return response;
        }

        await _unitOfWork.SaveChangesAndDispatchAsync(cancellationToken).ConfigureAwait(false);
        return response;
    }
}
