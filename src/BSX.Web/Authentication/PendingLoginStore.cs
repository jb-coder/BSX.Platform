using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace BSX.Web.Authentication;

/// <summary>
/// Holds verified sign-ins that are waiting for explicit workspace selection. Entries are kept for
/// a short window and are only referenced by an opaque token in the selection page URL.
/// </summary>
public sealed class PendingLoginStore
{
    private readonly ConcurrentDictionary<string, PendingLogin> _pending = new(StringComparer.Ordinal);

    /// <summary>Stores the pending sign-in and returns the opaque selection token.</summary>
    /// <param name="pendingLogin">The pending sign-in.</param>
    public string Save(PendingLogin pendingLogin)
    {
        ArgumentNullException.ThrowIfNull(pendingLogin);
        RemoveExpired();

        string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _pending[token] = pendingLogin;
        return token;
    }

    /// <summary>Reads a pending sign-in by selection token.</summary>
    /// <param name="token">The opaque selection token.</param>
    public PendingLogin? Load(string? token)
    {
        RemoveExpired();

        if (string.IsNullOrWhiteSpace(token)
            || !_pending.TryGetValue(token, out PendingLogin? pending))
        {
            return null;
        }

        if (pending.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            _pending.TryRemove(token, out _);
            return null;
        }

        return pending;
    }

    /// <summary>Removes a pending sign-in.</summary>
    /// <param name="token">The opaque selection token.</param>
    public void Remove(string? token)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            _pending.TryRemove(token, out _);
        }
    }

    private void RemoveExpired()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        foreach (KeyValuePair<string, PendingLogin> entry in _pending)
        {
            if (entry.Value.ExpiresAtUtc <= now)
            {
                _pending.TryRemove(entry.Key, out _);
            }
        }
    }
}
