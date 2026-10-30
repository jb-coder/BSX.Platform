namespace BSX.BuildingBlocks.Persistence;

/// <summary>
/// Queues side effects that must run after the command transaction has completed (ADR-022).
/// Used for security state that must persist even when a command returns a failed result.
/// </summary>
public interface IPostCommandActionQueue
{
    /// <summary>Queues an action to run after the command transaction completes.</summary>
    /// <param name="action">The action.</param>
    void Enqueue(Func<CancellationToken, Task> action);

    /// <summary>Runs every queued action in order.</summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task FlushAsync(CancellationToken cancellationToken = default);
}
