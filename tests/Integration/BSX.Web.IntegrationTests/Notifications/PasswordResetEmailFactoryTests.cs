using BSX.Web.Notifications;

namespace BSX.Web.IntegrationTests.Notifications;

public sealed class PasswordResetEmailFactoryTests
{
    [Fact]
    public void Should_Build_AnAbsoluteOneTimeResetLink()
    {
        PasswordResetDeliveryOptions options = CreateOptions();

        string resetUrl = PasswordResetEmailFactory.BuildResetUrl(options, "token-value");

        resetUrl.Should().Be("https://app.example.com/reset-password?token=token-value");
    }

    [Fact]
    public void Should_Render_LinkExpiry_AndGuidance_InHtmlAndText()
    {
        PasswordResetDeliveryOptions options = CreateOptions();
        var expiresAt = new DateTimeOffset(2026, 1, 2, 15, 30, 0, TimeSpan.Zero);

        PasswordResetEmail email = PasswordResetEmailFactory.Create(
            options,
            "user@example.com",
            "token-value",
            expiresAt);

        email.Subject.Should().Be("Reset your BSX password");
        email.ResetUrl.Should().Be("https://app.example.com/reset-password?token=token-value");
        email.HtmlBody.Should().Contain("https://app.example.com/reset-password?token=token-value");
        email.HtmlBody.Should().Contain("January 2, 2026 at 15:30 UTC");
        email.HtmlBody.Should().Contain("can be used once");
        email.HtmlBody.Should().Contain("If you did not request");
        email.TextBody.Should().Contain("https://app.example.com/reset-password?token=token-value");
        email.TextBody.Should().Contain("January 2, 2026 at 15:30 UTC");
    }

    [Fact]
    public void Should_ReportDeliveryUnavailable_UntilTheCompleteConfigurationIsPresent()
    {
        var options = new PasswordResetDeliveryOptions();
        options.IsConfigured.Should().BeFalse();

        options.Enabled = true;
        options.IsConfigured.Should().BeFalse("a public URL, SMTP host and sender are required");

        options.PublicBaseUrl = "https://app.example.com";
        options.Host = "smtp.example.com";
        options.FromAddress = "no-reply@example.com";

        options.IsConfigured.Should().BeTrue();
    }

    private static PasswordResetDeliveryOptions CreateOptions() => new()
    {
        Enabled = true,
        PublicBaseUrl = "https://app.example.com/",
        Host = "smtp.example.com",
        FromAddress = "no-reply@example.com",
        FromName = "BSX Platform",
    };
}
