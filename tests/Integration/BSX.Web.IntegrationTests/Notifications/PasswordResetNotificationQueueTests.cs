using System.Diagnostics;
using BSX.Web.Notifications;
using Microsoft.Extensions.Options;

namespace BSX.Web.IntegrationTests.Notifications;

public sealed class PasswordResetNotificationQueueTests
{
    [Fact]
    public async Task QueuedNotifier_ShouldQueueWithoutWaitingForSmtpDelivery()
    {
        var queue = new PasswordResetNotificationQueue();
        var notifier = new QueuedPasswordResetNotifier(
            Options.Create(new PasswordResetDeliveryOptions
            {
                Enabled = true,
                PublicBaseUrl = "https://app.example.com",
                Host = "smtp.example.com",
                FromAddress = "no-reply@example.com",
            }),
            queue);

        var stopwatch = Stopwatch.StartNew();
        await notifier.NotifyAsync(
            "user@example.com",
            "token-value",
            DateTimeOffset.UtcNow.AddMinutes(30));
        stopwatch.Stop();

        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await using var enumerator = queue.ReadAllAsync(cts.Token).GetAsyncEnumerator();
        (await enumerator.MoveNextAsync()).Should().BeTrue();
        enumerator.Current.Email.Should().Be("user@example.com");
        enumerator.Current.Token.Should().Be("token-value");
    }
}
