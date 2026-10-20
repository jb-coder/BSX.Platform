namespace BSX.Modules.Identity.Application.Abstractions;

/// <summary>
/// Delivers password reset tokens out of band (ADR-016). The plaintext token is passed in memory
/// only and must never be persisted by the implementation (for example in an outbox).
/// </summary>
public interface IPasswordResetNotifier
{
    /// <summary>
    /// Gets a value indicating whether password-reset delivery is available in the current
    /// deployment. When <see langword="false"/>, the request endpoint must fail explicitly rather
    /// than reporting a success that could never be delivered (ADR-016).
    /// </summary>
    bool IsDeliveryEnabled => true;

    /// <summary>Delivers a reset token to the user.</summary>
    /// <param name="email">The recipient email.</param>
    /// <param name="resetToken">The plaintext reset token.</param>
    /// <param name="expiresAtUtc">The token expiry.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task NotifyAsync(string email, string resetToken, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken = default);
}
