using BSX.Modules.Identity.Application.Abstractions;

namespace BSX.Modules.Identity.Application.UnitTests.Fakes;

internal sealed class FakePasswordResetNotifier : IPasswordResetNotifier
{
    public List<(string Email, string Token, DateTimeOffset ExpiresAtUtc)> Notifications { get; } = [];

    public Task NotifyAsync(string email, string resetToken, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken = default)
    {
        Notifications.Add((email, resetToken, expiresAtUtc));
        return Task.CompletedTask;
    }
}
