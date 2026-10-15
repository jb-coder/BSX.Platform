using System.Net;
using System.Net.Http.Json;

namespace BSX.Web.IntegrationTests;

/// <summary>
/// End-to-end coverage for password-reset token expiry. Uses its own host so that advancing the
/// clock cannot leak into any other test.
/// </summary>
public sealed class PasswordResetExpiryEndpointsTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public PasswordResetExpiryEndpointsTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task CompleteReset_Should_RejectExpiredToken()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage request = await client.PostAsJsonAsync(
            "/identity/password-reset/request",
            new { email = IdentityApiFactory.RestrictedEmail });
        request.StatusCode.Should().Be(HttpStatusCode.OK);

        string? token = _factory.ResetTokens.TokenFor(IdentityApiFactory.RestrictedEmail);
        token.Should().NotBeNullOrWhiteSpace();

        // The reset-token lifetime is 30 minutes; move past it.
        _factory.Clock.Advance(TimeSpan.FromMinutes(31));

        HttpResponseMessage complete = await client.PostAsJsonAsync(
            "/identity/password-reset/complete",
            new { token, newPassword = "Expired1!" });

        complete.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        complete.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }
}
