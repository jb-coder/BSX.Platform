using System.Net;
using System.Net.Http.Json;

namespace BSX.Web.IntegrationTests;

/// <summary>
/// Verifies the endpoint is safe and explicit when password-reset delivery is not configured
/// (D3): it must not report a misleading success.
/// </summary>
public sealed class PasswordResetDeliveryTests : IClassFixture<DisabledPasswordResetIdentityApiFactory>
{
    private readonly DisabledPasswordResetIdentityApiFactory _factory;

    public PasswordResetDeliveryTests(DisabledPasswordResetIdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task RequestReset_Should_ReturnServiceUnavailable_WhenDeliveryDisabled()
    {
        HttpResponseMessage response = await _factory.CreateClient().PostAsJsonAsync(
            "/identity/password-reset/request",
            new { email = "anyone@example.com" });

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }
}
