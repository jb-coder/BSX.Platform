using BSX.BuildingBlocks.Persistence;

namespace BSX.Modules.Identity.Application.UnitTests.Fakes;

/// <summary>Captures post-command actions so tests can flush them explicitly.</summary>
internal sealed class FakePostCommandActionQueue : IPostCommandActionQueue
{
    private readonly List<Func<CancellationToken, Task>> _actions = [];

    public int Count => _actions.Count;

    public void Enqueue(Func<CancellationToken, Task> action) => _actions.Add(action);

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        Func<CancellationToken, Task>[] actions = _actions.ToArray();
        _actions.Clear();

        foreach (Func<CancellationToken, Task> action in actions)
        {
            await action(cancellationToken);
        }
    }
}
