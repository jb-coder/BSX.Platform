namespace BSX.BuildingBlocks.Persistence;

/// <summary>Scoped queue of post-command actions (ADR-022).</summary>
internal sealed class PostCommandActionQueue : IPostCommandActionQueue
{
    private readonly List<Func<CancellationToken, Task>> _actions = [];

    /// <inheritdoc />
    public void Enqueue(Func<CancellationToken, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _actions.Add(action);
    }

    /// <inheritdoc />
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        Func<CancellationToken, Task>[] actions = _actions.ToArray();
        _actions.Clear();

        foreach (Func<CancellationToken, Task> action in actions)
        {
            await action(cancellationToken).ConfigureAwait(false);
        }
    }
}
