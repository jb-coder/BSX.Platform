using BSX.Modules.Identity.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace BSX.Web.Notifications;

/// <summary>
/// Password-reset notifier that hands delivery to the background email dispatcher. The request path
/// performs only the token persistence and an in-memory enqueue, so account existence cannot be
/// inferred from SMTP latency.
/// </summary>
internal sealed class QueuedPasswordResetNotifier : IPasswordResetNotifier
{
    private readonly PasswordResetDeliveryOptions _options;
    private readonly PasswordResetNotificationQueue _queue;

    /// <summary>Initializes a new instance of the <see cref="QueuedPasswordResetNotifier"/> class.</summary>
    public QueuedPasswordResetNotifier(
        IOptions<PasswordResetDeliveryOptions> options,
        PasswordResetNotificationQueue queue)
    {
        _options = options.Value;
        _queue = queue;
    }

    /// <inheritdoc />
    public bool IsDeliveryEnabled => _options.IsConfigured;

    /// <inheritdoc />
    public Task NotifyAsync(
        string email,
        string resetToken,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (!IsDeliveryEnabled)
        {
            throw new InvalidOperationException(
                "Password-reset delivery is not configured. The request handler must not invoke this notifier.");
        }

        return _queue
            .EnqueueAsync(new PasswordResetNotification(email, resetToken, expiresAtUtc), cancellationToken)
            .AsTask();
    }
}
