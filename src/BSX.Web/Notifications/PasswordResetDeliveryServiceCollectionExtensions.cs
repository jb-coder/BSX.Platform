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
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddWebPasswordResetDelivery(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<PasswordResetDeliveryOptions>(
            configuration.GetSection(PasswordResetDeliveryOptions.SectionName));
        services.Replace(
            ServiceDescriptor.Singleton<IPasswordResetNotifier, SmtpPasswordResetNotifier>());

        return services;
    }
}
