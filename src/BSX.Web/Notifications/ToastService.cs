namespace BSX.Web.Notifications;

/// <summary>Default in-process toast service; one instance per interactive user circuit.</summary>
public sealed class ToastService : IToastService, IDisposable
{
    private readonly List<ToastMessage> _messages = [];
    private readonly List<CancellationTokenSource> _timers = [];
    private readonly Lock _gate = new();

    /// <inheritdoc />
    public event Action? Changed;

    /// <inheritdoc />
    public IReadOnlyList<ToastMessage> Messages
    {
        get
        {
            lock (_gate)
            {
                return _messages.ToArray();
            }
        }
    }

    /// <inheritdoc />
    public void Show(ToastKind kind, string title, string? message = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        var toast = new ToastMessage(Guid.NewGuid(), kind, title, message);

        lock (_gate)
        {
            _messages.Add(toast);
        }

        Changed?.Invoke();
        ScheduleDismiss(toast.Id);
    }

    /// <inheritdoc />
    public void Success(string title, string? message = null) => Show(ToastKind.Success, title, message);

    /// <inheritdoc />
    public void Error(string title, string? message = null) => Show(ToastKind.Error, title, message);

    /// <inheritdoc />
    public void Dismiss(Guid id)
    {
        lock (_gate)
        {
            if (_messages.RemoveAll(message => message.Id == id) == 0)
            {
                return;
            }
        }

        Changed?.Invoke();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_gate)
        {
            foreach (CancellationTokenSource timer in _timers)
            {
                timer.Cancel();
                timer.Dispose();
            }

            _timers.Clear();
        }
    }

    private void ScheduleDismiss(Guid id)
    {
        var timer = new CancellationTokenSource();
        lock (_gate)
        {
            _timers.Add(timer);
        }

        _ = DismissAfterDelayAsync(id, timer);
    }

    private async Task DismissAfterDelayAsync(Guid id, CancellationTokenSource timer)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), timer.Token).ConfigureAwait(false);
            Dismiss(id);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            lock (_gate)
            {
                _timers.Remove(timer);
            }

            timer.Dispose();
        }
    }
}
