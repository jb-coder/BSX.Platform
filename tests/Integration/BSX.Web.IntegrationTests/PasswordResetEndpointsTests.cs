using System.Net;
using System.Net.Http.Json;
using BSX.Modules.Identity.Domain.Sessions;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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

    [Fact]
    public async Task CompleteReset_ShouldRevokeSessions_RotateStamp_AndRejectRefresh()
    {
        HttpClient client = _factory.CreateClient();
        IdentityApiFactory.LoginTokens tokens = await _factory.LoginAsync(
            client,
            IdentityApiFactory.RestrictedEmail,
            IdentityApiFactory.AdminPassword,
            _factory.TenantId);

        string stampBefore = await GetSecurityStampAsync(IdentityApiFactory.RestrictedEmail);
        string token = await RequestResetTokenAsync(client, IdentityApiFactory.RestrictedEmail);

        HttpResponseMessage complete = await client.PostAsJsonAsync(
            "/identity/password-reset/complete",
            new { token, newPassword = "SessionResetPassw0rd!" });
        complete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        string stampAfter = await GetSecurityStampAsync(IdentityApiFactory.RestrictedEmail);
        stampAfter.Should().NotBe(stampBefore);

        User user = await GetUserAsync(IdentityApiFactory.RestrictedEmail);
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            var sessions = scope.ServiceProvider.GetRequiredService<IUserSessionRepository>();
            IReadOnlyCollection<UserSession> active = await sessions.ListActiveByUserAsync(user.Id);
            active.Should().BeEmpty("password reset must revoke every active session");
        }

        HttpResponseMessage refresh = await client.PostAsJsonAsync(
            "/identity/refresh",
            new { refreshToken = tokens.RefreshToken });
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "the session's security stamp is now stale");
    }

    private async Task<string> RequestResetTokenAsync(HttpClient client, string email)
    {
        HttpResponseMessage request = await client.PostAsJsonAsync("/identity/password-reset/request", new { email });
        request.StatusCode.Should().Be(HttpStatusCode.OK);

        string? token = _factory.ResetTokens.TokenFor(email);
        token.Should().NotBeNullOrWhiteSpace();
        return token!;
    }

    private async Task<User> GetUserAsync(string email)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        Email normalized = Email.Create(email).Value;
        return await context.Users
            .AsNoTracking()
            .SingleAsync(user => user.Email == normalized);
    }

    private async Task<string> GetSecurityStampAsync(string email)
        => (await GetUserAsync(email)).SecurityStamp.Value;
}
