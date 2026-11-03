using System.Threading.Channels;

namespace BSX.Web.Notifications;

/// <summary>A single password-reset delivery job. It exists only in memory and is never logged.</summary>
internal sealed record PasswordResetNotification(string Email, string Token, DateTimeOffset ExpiresAtUtc);

/// <summary>In-process queue that keeps SMTP latency out of the password-reset request path.</summary>
internal sealed class PasswordResetNotificationQueue
{
    private readonly Channel<PasswordResetNotification> _channel =
        Channel.CreateUnbounded<PasswordResetNotification>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
            });

    /// <summary>Queues a reset email for asynchronous delivery.</summary>
    public ValueTask EnqueueAsync(
        PasswordResetNotification notification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        return _channel.Writer.WriteAsync(notification, cancellationToken);
    }

    /// <summary>Reads queued reset emails until the application stops.</summary>
    public IAsyncEnumerable<PasswordResetNotification> ReadAllAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAllAsync(cancellationToken);
}
