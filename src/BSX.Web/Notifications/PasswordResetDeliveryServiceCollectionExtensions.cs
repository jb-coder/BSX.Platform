using BSX.Modules.Identity.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BSX.Web.Notifications;

/// <summary>Registers the host-level SMTP password-reset delivery adapter.</summary>
public static class PasswordResetDeliveryServiceCollectionExtensions
{
    /// <summary>
    /// Replaces the disabled Identity default with the SMTP adapter. The adapter reports delivery as
    /// disabled unless the complete SMTP and public URL configuration is present.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="isDevelopment">Whether the host runs in the Development environment.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddWebPasswordResetDelivery(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<PasswordResetDeliveryOptions>()
            .Bind(configuration.GetSection(PasswordResetDeliveryOptions.SectionName))
            .Validate(
                options => !options.Enabled || isDevelopment || options.IsTransportSecure,
                "Password-reset delivery requires an HTTPS PublicBaseUrl and SMTP TLS outside Development.")
            .ValidateOnStart();

        services.TryAddSingleton<PasswordResetNotificationQueue>();
        services.TryAddSingleton<SmtpPasswordResetEmailSender>();
        services.Replace(
            ServiceDescriptor.Singleton<IPasswordResetNotifier, QueuedPasswordResetNotifier>());
        services.AddHostedService<PasswordResetEmailDispatcher>();

        return services;
    }
}
