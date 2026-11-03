using System.Net.Mail;

namespace BSX.Web.Notifications;

/// <summary>
/// SMTP delivery settings for password-reset emails. Delivery is disabled by default so a missing
/// or incomplete configuration fails closed with an explicit unavailable response instead of
/// pretending an email was sent.
/// </summary>
public sealed class PasswordResetDeliveryOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Identity:PasswordReset:Delivery";

    /// <summary>Gets or sets a value indicating whether password-reset delivery is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the externally reachable application base URL used to build reset links, for
    /// example <c>https://app.example.com</c>.
    /// </summary>
    public string PublicBaseUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the reset page path.</summary>
    public string ResetPath { get; set; } = "/reset-password";

    /// <summary>Gets or sets the SMTP host.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>Gets or sets the SMTP port.</summary>
    public int Port { get; set; } = 587;

    /// <summary>Gets or sets a value indicating whether SMTP transport security is enabled.</summary>
    public bool UseStartTls { get; set; } = true;

    /// <summary>Gets or sets the optional SMTP username.</summary>
    public string? UserName { get; set; }

    /// <summary>Gets or sets the optional SMTP password.</summary>
    public string? Password { get; set; }

    /// <summary>Gets or sets the envelope sender address.</summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>Gets or sets the sender display name.</summary>
    public string FromName { get; set; } = "BSX Platform";

    /// <summary>Gets a value indicating whether the settings are complete enough to deliver email.</summary>
    public bool IsConfigured
    {
        get
        {
            if (!Enabled
                || string.IsNullOrWhiteSpace(Host)
                || Port is < 1 or > 65535
                || string.IsNullOrWhiteSpace(PublicBaseUrl)
                || string.IsNullOrWhiteSpace(FromAddress))
            {
                return false;
            }

            if (!Uri.TryCreate(PublicBaseUrl, UriKind.Absolute, out Uri? baseUri)
                || (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
            {
                return false;
            }

            try
            {
                _ = new MailAddress(FromAddress);
                return true;
            }
            catch (Exception exception) when (exception is FormatException or ArgumentException)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the reset link and SMTP transport are both protected. This
    /// is required outside Development; Development may explicitly use HTTP/local SMTP.
    /// </summary>
    public bool IsTransportSecure
        => Uri.TryCreate(PublicBaseUrl, UriKind.Absolute, out Uri? baseUri)
            && baseUri.Scheme == Uri.UriSchemeHttps
            && UseStartTls;
}
