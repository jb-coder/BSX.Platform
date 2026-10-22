namespace BSX.BuildingBlocks.Persistence.Outbox;

/// <summary>
/// Storage seam for the transactional outbox (ADR-009). Each module registers one store backed by
/// its own <c>DbContext</c>. Claiming is atomic so a message is dispatched by exactly one worker
/// per attempt, even when several instances poll the same database.
/// </summary>
public interface IOutboxStore
{
    /// <summary>Gets the owning module name.</summary>
    string ModuleName { get; }

    /// <summary>
    /// Atomically claims pending messages for the supplied event identifiers. Messages that were
    /// already claimed or processed are not returned.
    /// </summary>
    /// <param name="eventIds">The event identifiers to claim.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The claimed envelopes.</returns>
    Task<IReadOnlyList<OutboxEnvelope>> ClaimAsync(
        IReadOnlyCollection<Guid> eventIds,
        CancellationToken cancellationToken = default);

    /// <summary>Reads pending or retryable messages without claiming them.</summary>
    /// <param name="batchSize">The maximum number of messages to read.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The pending envelopes.</returns>
    Task<IReadOnlyList<OutboxEnvelope>> ReadPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default);

    /// <summary>Marks messages as processed.</summary>
    /// <param name="eventIds">The event identifiers.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task MarkProcessedAsync(
        IReadOnlyCollection<Guid> eventIds,
        CancellationToken cancellationToken = default);

    /// <summary>Records a processing failure and schedules a retry or dead-letters the message.</summary>
    /// <param name="eventIds">The event identifiers.</param>
    /// <param name="error">The failure message.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task MarkFailedAsync(
        IReadOnlyCollection<Guid> eventIds,
        string error,
        CancellationToken cancellationToken = default);

    /// <summary>Counts pending or retryable messages.</summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The backlog count.</returns>
    Task<int> CountPendingAsync(CancellationToken cancellationToken = default);

    /// <summary>Requeues messages stuck in the processing state after a crash.</summary>
    /// <param name="staleAfter">How long a claim may remain outstanding.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The number of requeued messages.</returns>
    Task<int> RequeueStaleAsync(
        TimeSpan staleAfter,
        CancellationToken cancellationToken = default);
}
