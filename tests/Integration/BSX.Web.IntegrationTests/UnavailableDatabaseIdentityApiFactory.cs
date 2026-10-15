using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace BSX.Web.IntegrationTests;

/// <summary>
/// Boots the real host against an unreachable database (no seeding) so the readiness health check
/// reports the identity database as unavailable.
/// </summary>
public sealed class UnavailableDatabaseIdentityApiFactory : WebApplicationFactory<Program>
{
    private readonly RSA _rsa = RSA.Create(2048);

    /// <summary>Initializes a new instance of the <see cref="UnavailableDatabaseIdentityApiFactory"/> class.</summary>
    public UnavailableDatabaseIdentityApiFactory() => _ = CreateClient();

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            // Port 1 on loopback is never listening, so the connection attempt fails fast.
            ["ConnectionStrings:Identity"] =
                "Host=127.0.0.1;Port=1;Database=unreachable;Username=postgres;Password=postgres;Timeout=1;Command Timeout=1",
            ["Identity:Jwt:Issuer"] = "bsx-tests",
            ["Identity:Jwt:Audience"] = "bsx-tests",
            ["Identity:Jwt:SigningKeys:0:Kid"] = "test",
            ["Identity:Jwt:SigningKeys:0:PrivateKeyPem"] = _rsa.ExportRSAPrivateKeyPem(),
        }));
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _rsa.Dispose();
        }
    }
}
