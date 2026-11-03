using BSX.Modules.Identity.Application.Authentication;

namespace BSX.Modules.Identity.Application.UnitTests.Fakes;

internal sealed class FakePendingLoginStore : IPendingLoginStore
{
    private readonly Dictionary<string, PendingLogin> _pending = new(StringComparer.Ordinal);
    private int _counter;

    public PendingLoginHandle Seed(PendingLogin pendingLogin) => Save(pendingLogin);

    public PendingLoginHandle Save(PendingLogin pendingLogin)
    {
        _counter++;
        string selectionId = $"selection-{_counter}";
        string secretToken = $"secret-{_counter}";
        _pending[secretToken] = pendingLogin with { SelectionId = selectionId };
        return new PendingLoginHandle(selectionId, secretToken);
    }

    public PendingLogin? Get(string? secretToken)
        => !string.IsNullOrWhiteSpace(secretToken) && _pending.TryGetValue(secretToken, out PendingLogin? pending)
            ? pending
            : null;

    public PendingLogin? GetBySelectionId(string? selectionId)
        => _pending.Values.FirstOrDefault(pending =>
            string.Equals(pending.SelectionId, selectionId, StringComparison.Ordinal));

    public PendingLogin? Consume(string? secretToken)
    {
        if (string.IsNullOrWhiteSpace(secretToken)
            || !_pending.Remove(secretToken, out PendingLogin? pending))
        {
            return null;
        }

        return pending;
    }

    public void Remove(string? secretToken)
    {
        if (!string.IsNullOrWhiteSpace(secretToken))
        {
            _pending.Remove(secretToken);
        }
    }
}
