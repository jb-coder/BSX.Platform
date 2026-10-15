using System.Net;

namespace BSX.Web.IntegrationTests;

/// <summary>Verifies the readiness endpoint reports unhealthy when the database is unavailable.</summary>
public sealed class DatabaseHealthCheckTests : IClassFixture<UnavailableDatabaseIdentityApiFactory>
{
    private readonly UnavailableDatabaseIdentityApiFactory _factory;

    public DatabaseHealthCheckTests(UnavailableDatabaseIdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_Should_BeUnhealthy_WhenDatabaseUnavailable()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }
}

/// <summary>Verifies the readiness endpoint fails closed when authentication is misconfigured.</summary>
public sealed class AuthenticationHealthCheckTests : IClassFixture<MisconfiguredAuthenticationIdentityApiFactory>
{
    private readonly MisconfiguredAuthenticationIdentityApiFactory _factory;

    public AuthenticationHealthCheckTests(MisconfiguredAuthenticationIdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_Should_NotBeReady_WhenSigningKeysMisconfigured()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync("/health");

        // The JWT key ring refuses to build from the unparsable signing key, so the host never
        // reports ready. The failure surfaces as a non-success readiness response.
        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.InternalServerError);
    }
}
