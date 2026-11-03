using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Microsoft.Extensions.Options;

namespace BSX.Web.Notifications;

/// <summary>SMTP sender used by the background password-reset dispatcher.</summary>
internal sealed class SmtpPasswordResetEmailSender
{
    private readonly PasswordResetDeliveryOptions _options;
    private readonly ILogger<SmtpPasswordResetEmailSender> _logger;

    /// <summary>Initializes a new instance of the <see cref="SmtpPasswordResetEmailSender"/> class.</summary>
    public SmtpPasswordResetEmailSender(
        IOptions<PasswordResetDeliveryOptions> options,
        ILogger<SmtpPasswordResetEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>Sends one reset email. Delivery failures are logged without recipient or token data.</summary>
    public async Task SendAsync(PasswordResetNotification notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        try
        {
            PasswordResetEmail content = PasswordResetEmailFactory.Create(
                _options,
                notification.Email,
                notification.Token,
                notification.ExpiresAtUtc);

            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromName),
                Subject = content.Subject,
                SubjectEncoding = Encoding.UTF8,
                Body = content.HtmlBody,
                BodyEncoding = Encoding.UTF8,
                IsBodyHtml = true,
            };

            message.To.Add(new MailAddress(notification.Email));
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
