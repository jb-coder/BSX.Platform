using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using BSX.Modules.Identity.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace BSX.Web.Notifications;

/// <summary>
/// SMTP implementation of the existing <see cref="IPasswordResetNotifier"/> port. It composes the
/// reset link and email template without persisting or logging the plaintext token. Delivery
/// failures are logged without recipient or token data and do not reveal whether an account exists.
/// </summary>
internal sealed class SmtpPasswordResetNotifier : IPasswordResetNotifier
{
    private readonly PasswordResetDeliveryOptions _options;
    private readonly ILogger<SmtpPasswordResetNotifier> _logger;

    /// <summary>Initializes a new instance of the <see cref="SmtpPasswordResetNotifier"/> class.</summary>
    public SmtpPasswordResetNotifier(
        IOptions<PasswordResetDeliveryOptions> options,
        ILogger<SmtpPasswordResetNotifier> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsDeliveryEnabled => _options.IsConfigured;

    /// <inheritdoc />
    public async Task NotifyAsync(
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

        try
        {
            PasswordResetEmail content = PasswordResetEmailFactory.Create(
                _options,
                email,
                resetToken,
                expiresAtUtc);

            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromName),
                Subject = content.Subject,
                SubjectEncoding = Encoding.UTF8,
                Body = content.HtmlBody,
                BodyEncoding = Encoding.UTF8,
                IsBodyHtml = true,
            };

            message.To.Add(new MailAddress(email));
            message.AlternateViews.Add(
                AlternateView.CreateAlternateViewFromString(
                    content.TextBody,
                    Encoding.UTF8,
                    MediaTypeNames.Text.Plain));

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.UseStartTls,
                DeliveryMethod = SmtpDeliveryMethod.Network,
            };

            if (!string.IsNullOrWhiteSpace(_options.UserName))
            {
                client.Credentials = new NetworkCredential(_options.UserName, _options.Password);
            }

            await client
                .SendMailAsync(message, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Never log the recipient or reset token. The request remains enumeration-safe and the
            // operator can investigate delivery failures from the non-sensitive log entry.
            _logger.LogWarning(exception, "Password-reset email delivery failed.");
        }
    }
}
