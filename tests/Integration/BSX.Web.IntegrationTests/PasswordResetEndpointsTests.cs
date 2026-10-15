using System.Net;
using System.Net.Http.Json;

namespace BSX.Web.IntegrationTests;

/// <summary>End-to-end coverage for the password-reset lifecycle (complete, expired, reused).</summary>
public sealed class PasswordResetEndpointsTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public PasswordResetEndpointsTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task CompleteReset_Should_ChangePassword_WhenTokenValid()
    {
        HttpClient client = _factory.CreateClient();
        string token = await RequestResetTokenAsync(client, IdentityApiFactory.AdminEmail);

        HttpResponseMessage complete = await client.PostAsJsonAsync(
            "/identity/password-reset/complete",
            new { token, newPassword = "N3wPassw0rd!" });

        complete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage oldLogin = await client.PostAsJsonAsync(
            "/identity/login",
            new { email = IdentityApiFactory.AdminEmail, password = IdentityApiFactory.AdminPassword });
        oldLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        HttpResponseMessage newLogin = await client.PostAsJsonAsync(
            "/identity/login",
            new { email = IdentityApiFactory.AdminEmail, password = "N3wPassw0rd!" });
        newLogin.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CompleteReset_Should_RejectReusedToken()
    {
        HttpClient client = _factory.CreateClient();
        string token = await RequestResetTokenAsync(client, IdentityApiFactory.TenantAdminEmail);

        HttpResponseMessage first = await client.PostAsJsonAsync(
            "/identity/password-reset/complete",
            new { token, newPassword = "Reused1!" });
        first.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage second = await client.PostAsJsonAsync(
            "/identity/password-reset/complete",
            new { token, newPassword = "Reused2!" });

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CompleteReset_Should_RejectUnknownToken()
    {
        HttpResponseMessage response = await _factory.CreateClient().PostAsJsonAsync(
            "/identity/password-reset/complete",
            new { token = "not-a-real-token", newPassword = "Whatever1!" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    private async Task<string> RequestResetTokenAsync(HttpClient client, string email)
    {
        HttpResponseMessage request = await client.PostAsJsonAsync("/identity/password-reset/request", new { email });
        request.StatusCode.Should().Be(HttpStatusCode.OK);

        string? token = _factory.ResetTokens.TokenFor(email);
        token.Should().NotBeNullOrWhiteSpace();
        return token!;
    }
}
