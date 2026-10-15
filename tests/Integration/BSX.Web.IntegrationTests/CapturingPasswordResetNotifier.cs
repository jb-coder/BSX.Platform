using System.Collections.Concurrent;
using BSX.Modules.Identity.Application.Abstractions;

namespace BSX.Web.IntegrationTests;

/// <summary>
/// Test double for <see cref="IPasswordResetNotifier"/> that captures the plaintext reset token the
/// production notifier would deliver out of band, allowing an endpoint test to complete the reset.
/// </summary>
public sealed class CapturingPasswordResetNotifier : IPasswordResetNotifier
{
    private readonly ConcurrentDictionary<string, string> _tokens = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public Task NotifyAsync(
        string email,
        string resetToken,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        _tokens[email] = resetToken;
        return Task.CompletedTask;
    }

    /// <summary>Gets the most recently issued reset token for the given email, if any.</summary>
    /// <param name="email">The recipient email.</param>
    public string? TokenFor(string email)
        => _tokens.TryGetValue(email, out string? token) ? token : null;
}
