namespace BSX.Web.Notifications;

/// <summary>
/// Consumes queued password-reset emails outside the request path. SMTP latency and failures
/// therefore cannot be used to distinguish known accounts from unknown accounts.
/// </summary>
internal sealed class PasswordResetEmailDispatcher : BackgroundService
{
    private readonly PasswordResetNotificationQueue _queue;
    private readonly SmtpPasswordResetEmailSender _sender;
    private readonly ILogger<PasswordResetEmailDispatcher> _logger;

    /// <summary>Initializes a new instance of the <see cref="PasswordResetEmailDispatcher"/> class.</summary>
    public PasswordResetEmailDispatcher(
        PasswordResetNotificationQueue queue,
        SmtpPasswordResetEmailSender sender,
        ILogger<PasswordResetEmailDispatcher> logger)
    {
        _queue = queue;
        _sender = sender;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (PasswordResetNotification notification in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await _sender.SendAsync(notification, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Password-reset email dispatch failed.");
            }
        }
    }
}
