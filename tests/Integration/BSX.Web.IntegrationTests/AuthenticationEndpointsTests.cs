using System.Net;
using System.Net.Http.Headers;

namespace BSX.Web.IntegrationTests;

/// <summary>End-to-end coverage for the JWT authentication gate (fail-closed).</summary>
public sealed class AuthenticationEndpointsTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public AuthenticationEndpointsTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task ProtectedEndpoint_Should_RejectMalformedToken()
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-valid-jwt");

        HttpResponseMessage response = await client.GetAsync("/identity/sessions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_Should_RejectMissingToken()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync("/identity/sessions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
