using BSX.Modules.Identity.Infrastructure.Jwt;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BSX.Web.IntegrationTests;

/// <summary>
/// Boots the real host with an unparsable JWT signing key. The JWT key ring cannot be constructed,
/// so the authentication readiness check reports the host as not ready (fail-fast/fail-closed).
/// </summary>
public sealed class MisconfiguredAuthenticationIdentityApiFactory : IdentityApiFactory
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<JwtOptions>();
            var options = new JwtOptions { Issuer = "bsx-tests", Audience = "bsx-tests" };
            options.SigningKeys.Add(new JwtSigningKey
            {
                Kid = "test",
                PrivateKeyPem = "-----BEGIN PRIVATE KEY-----\nnot-a-valid-key\n-----END PRIVATE KEY-----",
            });

            services.AddSingleton(options);
        });
    }
}
