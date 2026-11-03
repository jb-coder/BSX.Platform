using BSX.Web.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BSX.Web.IntegrationTests.Notifications;

public sealed class PasswordResetDeliveryConfigurationTests
{
    [Fact]
    public void IsTransportSecure_ShouldRequireHttpsAndSmtpTls()
    {
        PasswordResetDeliveryOptions insecureUrl = CreateOptions("http://app.example.com", useStartTls: true);
        PasswordResetDeliveryOptions insecureSmtp = CreateOptions("https://app.example.com", useStartTls: false);
        PasswordResetDeliveryOptions secure = CreateOptions("https://app.example.com", useStartTls: true);

        insecureUrl.IsTransportSecure.Should().BeFalse();
        insecureSmtp.IsTransportSecure.Should().BeFalse();
        secure.IsTransportSecure.Should().BeTrue();
    }

    [Fact]
    public void Production_ShouldRejectAnHttpResetUrl()
    {
        ServiceProvider provider = BuildProvider(
            publicBaseUrl: "http://app.example.com",
            useStartTls: true,
            isDevelopment: false);

        Action act = () => _ = provider.GetRequiredService<IOptions<PasswordResetDeliveryOptions>>().Value;

        act.Should().Throw<OptionsValidationException>();
    }

    [Fact]
    public void Production_ShouldRejectSmtpWithoutTls()
    {
        ServiceProvider provider = BuildProvider(
            publicBaseUrl: "https://app.example.com",
            useStartTls: false,
            isDevelopment: false);

        Action act = () => _ = provider.GetRequiredService<IOptions<PasswordResetDeliveryOptions>>().Value;

        act.Should().Throw<OptionsValidationException>();
    }

    [Fact]
    public void Development_ShouldAllowLocalInsecureTransport()
    {
        ServiceProvider provider = BuildProvider(
            publicBaseUrl: "http://localhost:5272",
            useStartTls: false,
            isDevelopment: true);

        Action act = () => _ = provider.GetRequiredService<IOptions<PasswordResetDeliveryOptions>>().Value;

        act.Should().NotThrow();
    }

    [Fact]
    public void Production_ShouldAcceptHttpsAndSmtpTls()
    {
        ServiceProvider provider = BuildProvider(
            publicBaseUrl: "https://app.example.com",
            useStartTls: true,
            isDevelopment: false);

        Action act = () => _ = provider.GetRequiredService<IOptions<PasswordResetDeliveryOptions>>().Value;

        act.Should().NotThrow();
    }

    private static PasswordResetDeliveryOptions CreateOptions(string publicBaseUrl, bool useStartTls) => new()
    {
        Enabled = true,
        PublicBaseUrl = publicBaseUrl,
        Host = "smtp.example.com",
        UseStartTls = useStartTls,
        FromAddress = "no-reply@example.com",
    };

    private static ServiceProvider BuildProvider(string publicBaseUrl, bool useStartTls, bool isDevelopment)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{PasswordResetDeliveryOptions.SectionName}:Enabled"] = "true",
                [$"{PasswordResetDeliveryOptions.SectionName}:PublicBaseUrl"] = publicBaseUrl,
                [$"{PasswordResetDeliveryOptions.SectionName}:Host"] = "smtp.example.com",
                [$"{PasswordResetDeliveryOptions.SectionName}:UseStartTls"] = useStartTls.ToString(),
                [$"{PasswordResetDeliveryOptions.SectionName}:FromAddress"] = "no-reply@example.com",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWebPasswordResetDelivery(configuration, isDevelopment);
        return services.BuildServiceProvider();
    }
}
