using System.Collections.Concurrent;
using System.Security.Cryptography;
using BSX.Modules.Identity.Application.Authentication;

namespace BSX.Web.Authentication;

/// <summary>
/// Server-side store for verified sign-ins that are waiting for explicit workspace selection.
/// Entries are short-lived, keyed by a secret held only in an HttpOnly cookie, and never contain
/// the user's password. The non-secret selection handle is only used to render the chooser.
/// </summary>
public sealed class PendingLoginStore : IPendingLoginStore
{
    private const int SelectionIdByteLength = 32;
    private const int SecretTokenByteLength = 32;

    private readonly ConcurrentDictionary<string, PendingLogin> _pending = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _selectionToSecret = new(StringComparer.Ordinal);
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="PendingLoginStore"/> class.</summary>
    /// <param name="timeProvider">The time provider used for expiry checks.</param>
    public PendingLoginStore(TimeProvider timeProvider) => _timeProvider = timeProvider;

    /// <summary>Initializes a store backed by the system clock.</summary>
    public PendingLoginStore()
        : this(TimeProvider.System)
    {
    }

    /// <inheritdoc />
    public PendingLoginHandle Save(PendingLogin pendingLogin)
    {
        ArgumentNullException.ThrowIfNull(pendingLogin);
        RemoveExpired();

        string selectionId = NewToken(SelectionIdByteLength);
        string secretToken = NewToken(SecretTokenByteLength);
        PendingLogin stored = pendingLogin with { SelectionId = selectionId };

        _pending[secretToken] = stored;
        _selectionToSecret[selectionId] = secretToken;

        return new PendingLoginHandle(selectionId, secretToken);
    }

    /// <inheritdoc />
    public PendingLogin? Get(string? secretToken)
    {
        RemoveExpired();

        if (string.IsNullOrWhiteSpace(secretToken)
            || !_pending.TryGetValue(secretToken, out PendingLogin? pending))
        {
            return null;
        }

        if (pending.ExpiresAtUtc <= _timeProvider.GetUtcNow())
        {
            Remove(secretToken);
            return null;
        }

        return pending;
    }

    /// <inheritdoc />
    public PendingLogin? GetBySelectionId(string? selectionId)
    {
        RemoveExpired();

        if (string.IsNullOrWhiteSpace(selectionId)
            || !_selectionToSecret.TryGetValue(selectionId, out string? secretToken))
        {
            return null;
        }

        return Get(secretToken);
    }

    /// <inheritdoc />
    public PendingLogin? Consume(string? secretToken)
    {
        RemoveExpired();

        if (string.IsNullOrWhiteSpace(secretToken)
            || !_pending.TryRemove(secretToken, out PendingLogin? pending))
        {
            return null;
        }

        _selectionToSecret.TryRemove(pending.SelectionId, out _);

        return pending.ExpiresAtUtc <= _timeProvider.GetUtcNow() ? null : pending;
    }

    /// <inheritdoc />
    public void Remove(string? secretToken)
    {
        if (string.IsNullOrWhiteSpace(secretToken)
            || !_pending.TryRemove(secretToken, out PendingLogin? pending))
        {
            return;
        }

        _selectionToSecret.TryRemove(pending.SelectionId, out _);
    }

    private void RemoveExpired()
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();

        foreach (KeyValuePair<string, PendingLogin> entry in _pending)
        {
            if (entry.Value.ExpiresAtUtc <= now)
            {
                Remove(entry.Key);
            }
        }
    }

    private static string NewToken(int byteLength)
        => Convert.ToHexString(RandomNumberGenerator.GetBytes(byteLength));
}
