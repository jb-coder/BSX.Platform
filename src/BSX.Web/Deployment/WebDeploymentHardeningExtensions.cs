using System.Globalization;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Options;

namespace BSX.Web.Deployment;

/// <summary>
/// Deployment hardening for reverse-proxy topologies: forwarded headers, Data Protection key
/// persistence, security headers and the deployment configuration checks that fail clearly.
/// </summary>
public static class WebDeploymentHardeningExtensions
{
    /// <summary>
    /// Registers reverse-proxy, Data Protection, security-header and deployment-health services.
    /// Configuration is bound when the host starts, so environment variables, secret stores and
    /// test-host overrides are all authoritative.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddWebDeploymentHardening(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<ReverseProxyOptions>()
            .Configure<IConfiguration>((options, configuration) =>
                configuration.GetSection(ReverseProxyOptions.SectionName).Bind(options));
        services.AddOptions<DataProtectionOptions>()
            .Configure<IConfiguration>((options, configuration) =>
                configuration.GetSection(DataProtectionOptions.SectionName).Bind(options));
        services.AddOptions<SecurityHeadersOptions>()
            .Configure<IConfiguration>((options, configuration) =>
                configuration.GetSection(SecurityHeadersOptions.SectionName).Bind(options));
        services.AddOptions<HstsOptions>()
            .Configure<IConfiguration>((options, configuration) =>
                configuration.GetSection("Hsts").Bind(options));

        services.AddOptions<ForwardedHeadersOptions>()
            .Configure<IOptions<ReverseProxyOptions>>((options, reverseProxy) =>
                ConfigureForwardedHeaders(options, reverseProxy.Value));

        services.AddDataProtection();

        services.AddSingleton<IConfigureOptions<Microsoft.AspNetCore.DataProtection.DataProtectionOptions>>(
            provider => new ConfigureOptions<Microsoft.AspNetCore.DataProtection.DataProtectionOptions>(
                options => options.ApplicationDiscriminator =
                    provider.GetRequiredService<IOptions<DataProtectionOptions>>().Value.ApplicationName));

        services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(
            provider => new ConfigureOptions<KeyManagementOptions>(options =>
            {
                DataProtectionOptions dataProtection = provider
                    .GetRequiredService<IOptions<DataProtectionOptions>>().Value;
                IHostEnvironment environment = provider.GetRequiredService<IHostEnvironment>();

                string keysPath = ResolveDataProtectionKeysPath(dataProtection, environment);
                options.XmlRepository = new FileSystemKeyRepository(
                    new DirectoryInfo(keysPath),
                    provider.GetRequiredService<ILogger<FileSystemKeyRepository>>());
            }));

        services.AddHealthChecks()
            .AddCheck<DataProtectionHealthCheck>("data-protection", tags: ["ready"]);

        return services;
    }

    /// <summary>
    /// Validates the deployment configuration, adds forwarded-header processing and applies the
    /// security response headers. Call this first in the pipeline so scheme, host and client-IP are
    /// corrected before HTTPS redirection, HSTS, authentication and authorization run.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The same builder for chaining.</returns>
    public static IApplicationBuilder UseWebDeploymentHardening(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        IServiceProvider services = app.ApplicationServices;
        IConfiguration configuration = services.GetRequiredService<IConfiguration>();
        IHostEnvironment environment = services.GetRequiredService<IHostEnvironment>();
        ReverseProxyOptions reverseProxy = services.GetRequiredService<IOptions<ReverseProxyOptions>>().Value;
        DataProtectionOptions dataProtection = services.GetRequiredService<IOptions<DataProtectionOptions>>().Value;

        ValidateAllowedHosts(configuration, environment);
        _ = ResolveDataProtectionKeysPath(dataProtection, environment);

        if (reverseProxy.Enabled)
        {
            app.UseForwardedHeaders();
        }

        app.UseSecurityHeaders();
        return app;
    }

    /// <summary>
    /// Resolves and validates the Data Protection key directory. Outside Development an explicit
    /// persistent path is required; the directory must exist or be creatable and writable.
    /// </summary>
    /// <param name="options">The Data Protection settings.</param>
    /// <param name="environment">The host environment.</param>
    /// <returns>The absolute, writable key directory.</returns>
    internal static string ResolveDataProtectionKeysPath(
        DataProtectionOptions options,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);

        string path = options.KeysPath ?? string.Empty;

        if (string.IsNullOrWhiteSpace(path))
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "DataProtection:KeysPath must be configured outside Development so authentication "
                    + "cookies and antiforgery tokens survive restarts. Mount a persistent directory or "
                    + "shared volume and set DataProtection:KeysPath.");
            }

            path = Path.Combine(environment.ContentRootPath, ".data-protection-keys");
        }

        string fullPath = Path.GetFullPath(
            Path.IsPathRooted(path) ? path : Path.Combine(environment.ContentRootPath, path));

        try
        {
            Directory.CreateDirectory(fullPath);
            string probe = Path.Combine(fullPath, $".write-test-{Guid.NewGuid():N}");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException
            or ArgumentException
            or NotSupportedException)
        {
            throw new InvalidOperationException(
                $"DataProtection:KeysPath '{fullPath}' is not a writable directory. Mount a persistent "
                + "volume with write access for the application process.",
                exception);
        }

        return fullPath;
    }

    /// <summary>
    /// Rejects wildcard or development-default AllowedHosts outside Development so Host-header
    /// filtering cannot be accidentally disabled in a deployment.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="environment">The host environment.</param>
    internal static void ValidateAllowedHosts(IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        if (environment.IsDevelopment())
        {
            return;
        }

        string? allowedHosts = configuration["AllowedHosts"];

        if (string.IsNullOrWhiteSpace(allowedHosts)
            || string.Equals(allowedHosts, "*", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "AllowedHosts must be set to the public host name(s) outside Development; '*' is not permitted.");
        }

        if (string.Equals(allowedHosts, "localhost;127.0.0.1", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "AllowedHosts is still the development default ('localhost;127.0.0.1'). "
                + "Configure the public host name(s) for this deployment.");
        }
    }

    private static void ConfigureForwardedHeaders(
        ForwardedHeadersOptions options,
        ReverseProxyOptions reverseProxy)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
            | ForwardedHeaders.XForwardedProto
            | ForwardedHeaders.XForwardedHost;
        options.ForwardLimit = Math.Max(1, reverseProxy.ForwardLimit);

        if (reverseProxy.KnownProxies.Length == 0 && reverseProxy.KnownNetworks.Length == 0)
        {
            // Keep the framework's loopback-only defaults. This is safe for same-host sidecars and
            // direct deployments; remote proxies must be configured explicitly.
            return;
        }

        options.KnownProxies.Clear();
        options.KnownIPNetworks.Clear();

        foreach (string proxy in reverseProxy.KnownProxies)
        {
            options.KnownProxies.Add(IPAddress.Parse(proxy));
        }

        foreach (string network in reverseProxy.KnownNetworks)
        {
            options.KnownIPNetworks.Add(ParseNetwork(network));
        }
    }

    private static System.Net.IPNetwork ParseNetwork(string value)
    {
        string[] parts = value.Split('/', 2);
        IPAddress prefix = IPAddress.Parse(parts[0]);
        int prefixLength = parts.Length == 2
            ? int.Parse(parts[1], CultureInfo.InvariantCulture)
            : prefix.AddressFamily == AddressFamily.InterNetwork ? 32 : 128;

        return new System.Net.IPNetwork(prefix, prefixLength);
    }
}
