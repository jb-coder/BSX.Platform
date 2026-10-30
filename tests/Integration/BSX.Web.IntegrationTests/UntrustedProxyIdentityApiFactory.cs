using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BSX.Web.IntegrationTests;

/// <summary>
/// Boots the host with a trusted proxy that is not the loopback test client. Forwarded headers
/// sent by the test client must therefore be ignored.
/// </summary>
public sealed class UntrustedProxyIdentityApiFactory : IdentityApiFactory
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ReverseProxy:KnownProxies:0"] = "10.0.0.1",
            }));

        builder.ConfigureServices(services => services.Configure<HttpsRedirectionOptions>(
            options => options.HttpsPort = 443));
    }
}
