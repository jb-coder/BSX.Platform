using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.WebUtilities;

namespace BSX.Web.Notifications;

/// <summary>The composed password-reset email. The plaintext token appears only inside the reset link.</summary>
internal sealed record PasswordResetEmail(string Subject, string HtmlBody, string TextBody, string ResetUrl);

/// <summary>Builds the password-reset email and its absolute reset link.</summary>
internal static class PasswordResetEmailFactory
{
    internal const string Subject = "Reset your BSX password";

    internal static PasswordResetEmail Create(
        PasswordResetDeliveryOptions options,
        string recipientEmail,
        string resetToken,
        DateTimeOffset expiresAtUtc)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(resetToken);

        string resetUrl = BuildResetUrl(options, resetToken);
        string encodedUrl = WebUtility.HtmlEncode(resetUrl);
        string expiry = expiresAtUtc
            .ToUniversalTime()
            .ToString("MMMM d, yyyy 'at' HH:mm 'UTC'", CultureInfo.InvariantCulture);

        string html = $"""
            <!doctype html>
            <html lang="en">
            <body style="margin:0;padding:0;background:#f4f6fb;font-family:Arial,Helvetica,sans-serif;color:#1f2937;">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#f4f6fb;padding:32px 16px;">
                <tr>
                  <td align="center">
                    <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:560px;background:#ffffff;border-radius:12px;padding:32px;">
                      <tr>
                        <td>
                          <p style="margin:0 0 8px;font-size:12px;letter-spacing:0.08em;text-transform:uppercase;color:#64748b;">BSX Platform</p>
                          <h1 style="margin:0 0 16px;font-size:24px;line-height:1.25;color:#0f172a;">Reset your password</h1>
                          <p style="margin:0 0 12px;font-size:16px;line-height:1.6;">We received a request to reset the password for your BSX account.</p>
                          <p style="margin:0 0 24px;font-size:16px;line-height:1.6;">Use the button below to choose a new password. This link expires on <strong>{WebUtility.HtmlEncode(expiry)}</strong> and can be used once.</p>
                          <p style="margin:0 0 24px;">
                            <a href="{encodedUrl}" style="display:inline-block;background:#2563eb;color:#ffffff;text-decoration:none;font-weight:600;padding:12px 20px;border-radius:8px;">Reset password</a>
                          </p>
                          <p style="margin:0 0 12px;font-size:14px;line-height:1.6;color:#475569;">If the button does not work, copy this link into your browser:</p>
                          <p style="margin:0 0 24px;font-size:13px;line-height:1.6;word-break:break-all;color:#2563eb;">{encodedUrl}</p>
                          <p style="margin:0;font-size:13px;line-height:1.6;color:#64748b;">If you did not request a password reset, you can ignore this email. Your password will not change.</p>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;

        string text = $"""
            Reset your BSX password

            We received a request to reset the password for your BSX account.

            Open this link to choose a new password:
            {resetUrl}

            This link expires on {expiry} and can be used once.

            If you did not request a password reset, you can ignore this email. Your password will not change.
            """;

        return new PasswordResetEmail(Subject, html, text, resetUrl);
    }

    internal static string BuildResetUrl(PasswordResetDeliveryOptions options, string resetToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(resetToken);

        string baseUrl = options.PublicBaseUrl.TrimEnd('/');
        string resetPath = string.IsNullOrWhiteSpace(options.ResetPath)
            ? "/reset-password"
            : options.ResetPath.StartsWith('/') ? options.ResetPath : "/" + options.ResetPath;

        return QueryHelpers.AddQueryString(baseUrl + resetPath, "token", resetToken);
    }
}
