using BSX.Modules.Identity.Application.Abstractions;

namespace BSX.Modules.Identity.Infrastructure.Security;

/// <summary>
/// Default <see cref="IPasswordResetNotifier"/> used when no delivery mechanism is configured.
/// It is intentionally <b>not</b> a silent success: <see cref="IsDeliveryEnabled"/> is
/// <see langword="false"/>, so the request endpoint fails explicitly (HTTP 503) rather than
/// reporting a success that could never be delivered (ADR-016).
/// The Notifications module (or the host) replaces this registration with a real notifier.
/// </summary>
internal sealed class DisabledPasswordResetNotifier : IPasswordResetNotifier
{
    /// <inheritdoc />
    public bool IsDeliveryEnabled => false;

    /// <inheritdoc />
    public Task NotifyAsync(string email, string resetToken, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(
            "Password-reset delivery is not configured. The request handler must not invoke this notifier.");
}
