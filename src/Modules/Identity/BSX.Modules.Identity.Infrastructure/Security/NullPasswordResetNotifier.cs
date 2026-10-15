using BSX.Modules.Identity.Application.Abstractions;

namespace BSX.Modules.Identity.Infrastructure.Security;

/// <summary>
/// No-op default notifier used until the Notifications module registers a real implementation.
/// Intentionally does not log the token (ADR-016: secrets are never logged or persisted).
/// </summary>
internal sealed class NullPasswordResetNotifier : IPasswordResetNotifier
{
    /// <inheritdoc />
    public Task NotifyAsync(string email, string resetToken, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
