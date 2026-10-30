using BSX.BuildingBlocks.Authorization;
using BSX.Web.Authorization;
using BSX.Web.Deployment;
using BSX.Web.Notifications;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BSX.Web.Authentication;

/// <summary>Registers the web authentication, authorization and application services.</summary>
public static class WebAuthenticationExtensions
{
    /// <summary>Adds cookie authentication for the Blazor app, JWT for the APIs and permission policies.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="isDevelopment">Whether the host runs in the Development environment.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddWebAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<AuthorizationRevalidationOptions>(
            configuration.GetSection(AuthorizationRevalidationOptions.SectionName));

        WebCookieOptions cookieOptions = BindCookieOptions(configuration, isDevelopment);
        services.AddSingleton(cookieOptions);

        services.AddAuthentication(options =>
            {
                options.DefaultScheme = WebAuthDefaults.SmartScheme;
                options.DefaultAuthenticateScheme = WebAuthDefaults.SmartScheme;
                options.DefaultChallengeScheme = WebAuthDefaults.SmartScheme;
                options.DefaultSignInScheme = WebAuthDefaults.CookieScheme;
            })
            .AddPolicyScheme(WebAuthDefaults.SmartScheme, "BSX smart authentication", options =>
            {
                options.ForwardDefaultSelector = SelectAuthenticationScheme;
            })
            .AddCookie(WebAuthDefaults.CookieScheme, options =>
            {
                options.Cookie.Name = WebAuthDefaults.AuthCookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = cookieOptions.AuthSameSite;
                options.Cookie.SecurePolicy = cookieOptions.SecurePolicy;
                options.Cookie.Domain = cookieOptions.Domain;
                options.LoginPath = "/login";
                options.AccessDeniedPath = "/access-denied";
                options.ReturnUrlParameter = "returnUrl";
                options.ExpireTimeSpan = WebAuthDefaults.SessionLifetime;
                options.SlidingExpiration = false;
                options.EventsType = typeof(WebCookieAuthenticationEvents);
            });

        services.AddAntiforgery(options =>
        {
            options.Cookie.Name = WebAuthDefaults.AntiforgeryCookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = cookieOptions.AntiforgerySameSite;
            options.Cookie.SecurePolicy = cookieOptions.SecurePolicy;
            options.Cookie.Domain = cookieOptions.Domain;
            options.HeaderName = "X-CSRF-TOKEN";
        });

        services.TryAddScoped<WebCookieAuthenticationEvents>();
        services.TryAddScoped<WebCurrentUser>();
        services.TryAddScoped<WebSessionState>();
        services.TryAddScoped<BSX.Web.Administration.AdministrationDispatcher>();
        services.Replace(ServiceDescriptor.Scoped<ICurrentUser>(
            provider => provider.GetRequiredService<WebCurrentUser>()));
        services.TryAddSingleton<PendingLoginStore>();
        services.TryAddScoped<IToastService, ToastService>();
        services.AddCascadingAuthenticationState();

        // Authorization revalidation (ADR-021): per-operation store check plus framework-aligned
        // circuit revalidation.
        services.TryAddScoped<WebAuthorizationRevalidator>();
        services.Replace(ServiceDescriptor.Scoped<IAuthorizationRevalidator>(
            provider => provider.GetRequiredService<WebAuthorizationRevalidator>()));
        services.Replace(ServiceDescriptor.Scoped<AuthenticationStateProvider, BsxRevalidatingAuthenticationStateProvider>());

        services.AddAuthorization();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IAuthorizationHandler, PermissionAuthorizationHandler>());

        return services;
    }

    private static WebCookieOptions BindCookieOptions(IConfiguration configuration, bool isDevelopment)
    {
        IConfigurationSection section = configuration.GetSection(WebCookieOptions.SectionName);
        var options = new WebCookieOptions();
        section.Bind(options);

        // Preserve the HTTP launch profile in Development. In every other environment the
        // production-safe default is Secure=Always, even if a proxy forwards the wrong scheme.
        if (isDevelopment && !section.Exists())
        {
            options.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        }

        return options;
    }

    private static string SelectAuthenticationScheme(HttpContext context)
        => IsApiRequest(context.Request.Path)
            ? JwtBearerDefaults.AuthenticationScheme
            : WebAuthDefaults.CookieScheme;

    private static bool IsApiRequest(PathString path)
        => path.StartsWithSegments("/identity")
            || path.StartsWithSegments("/crm")
            || path.StartsWithSegments("/sales")
            || path.StartsWithSegments("/health");
}
