using System.Net;
using System.Net.Http.Json;

namespace BSX.Web.IntegrationTests.WebFoundation;

public sealed class PasswordRecoveryWebTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public PasswordRecoveryWebTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task ForgotPasswordPage_ShouldRenderTheRequestForm()
    {
        HttpClient client = WebFoundationTestHelper.CreateClient(_factory);

        HttpResponseMessage response = await client.GetAsync("/forgot-password");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Forgot your password?");
        html.Should().Contain("name=\"email\"");
        html.Should().Contain("action=\"/auth/password-reset/request\"");
        html.Should().Contain("__RequestVerificationToken");
    }

    [Fact]
    public async Task ForgotPasswordRequest_ShouldSendNotification_AndNotDiscloseTheToken()
    {
        HttpClient client = WebFoundationTestHelper.CreateClient(_factory);
        string antiforgery = await WebFoundationTestHelper.GetAntiforgeryTokenAsync(client, "/forgot-password");

        HttpResponseMessage response = await WebFoundationTestHelper.PostFormAsync(
            client,
            "/auth/password-reset/request",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = antiforgery,
                ["email"] = IdentityApiFactory.AdminEmail,
            });

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        string location = WebFoundationTestHelper.LocationPath(response)!;
        location.Should().Be("/forgot-password?sent=true");
        location.Should().NotContain("token=");
        _factory.ResetTokens.TokenFor(IdentityApiFactory.AdminEmail).Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ForgotPasswordRequest_ShouldBeEnumerationSafe()
    {
        HttpClient knownClient = WebFoundationTestHelper.CreateClient(_factory);
        string knownAntiforgery = await WebFoundationTestHelper.GetAntiforgeryTokenAsync(knownClient, "/forgot-password");
        HttpResponseMessage known = await WebFoundationTestHelper.PostFormAsync(
            knownClient,
            "/auth/password-reset/request",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = knownAntiforgery,
                ["email"] = IdentityApiFactory.AdminEmail,
            });

        HttpClient unknownClient = WebFoundationTestHelper.CreateClient(_factory);
        string unknownAntiforgery = await WebFoundationTestHelper.GetAntiforgeryTokenAsync(unknownClient, "/forgot-password");
        HttpResponseMessage unknown = await WebFoundationTestHelper.PostFormAsync(
            unknownClient,
            "/auth/password-reset/request",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = unknownAntiforgery,
                ["email"] = "missing-recovery@example.com",
            });

        known.StatusCode.Should().Be(unknown.StatusCode);
        WebFoundationTestHelper.LocationPath(known).Should().Be(
            WebFoundationTestHelper.LocationPath(unknown));
    }

    [Fact]
    public async Task ResetPasswordPage_ShouldRenderForAValidToken()
    {
        HttpClient client = WebFoundationTestHelper.CreateClient(_factory);
        string token = await RequestResetAsync(client, IdentityApiFactory.RestrictedEmail);

        HttpResponseMessage response = await client.GetAsync($"/reset-password?token={Uri.EscapeDataString(token)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Choose a new password");
        html.Should().Contain("name=\"newPassword\"");
        html.Should().Contain("name=\"confirmPassword\"");
        html.Should().Contain("name=\"token\"");
        html.Should().Contain("action=\"/auth/password-reset/complete\"");
    }

    [Fact]
    public async Task CompleteReset_ShouldChangePassword_AndAllowSignInAgain()
    {
        HttpClient client = WebFoundationTestHelper.CreateClient(_factory);
        string token = await RequestResetAsync(client, IdentityApiFactory.RestrictedEmail);
        const string newPassword = "RecoveredPassw0rd!";

        string antiforgery = await WebFoundationTestHelper.GetAntiforgeryTokenAsync(
            client,
            $"/reset-password?token={Uri.EscapeDataString(token)}");

        HttpResponseMessage complete = await WebFoundationTestHelper.PostFormAsync(
            client,
            "/auth/password-reset/complete",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = antiforgery,
                ["token"] = token,
                ["newPassword"] = newPassword,
                ["confirmPassword"] = newPassword,
            });

        complete.StatusCode.Should().Be(HttpStatusCode.Redirect);
        WebFoundationTestHelper.LocationPath(complete).Should().Be("/login?reset=success");

        HttpClient oldLoginClient = _factory.CreateClient();
        HttpResponseMessage oldLogin = await oldLoginClient.PostAsJsonAsync(
            "/identity/login",
            new
            {
                email = IdentityApiFactory.RestrictedEmail,
                password = IdentityApiFactory.AdminPassword,
                tenantId = _factory.TenantId,
            });
        oldLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        HttpClient newLoginClient = _factory.CreateClient();
        HttpResponseMessage newLogin = await newLoginClient.PostAsJsonAsync(
            "/identity/login",
            new
            {
                email = IdentityApiFactory.RestrictedEmail,
                password = newPassword,
                tenantId = _factory.TenantId,
            });
        newLogin.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResetPasswordPage_ShouldExplainExpiredAndInvalidTokens()
    {
        HttpClient client = WebFoundationTestHelper.CreateClient(_factory);

        HttpResponseMessage expired = await client.GetAsync("/reset-password?error=expired");
        expired.StatusCode.Should().Be(HttpStatusCode.OK);
        string expiredHtml = await expired.Content.ReadAsStringAsync();
        expiredHtml.Should().Contain("expired");
        expiredHtml.Should().NotContain("name=\"newPassword\"");

        HttpResponseMessage invalid = await client.GetAsync("/reset-password?error=invalid_token");
        invalid.StatusCode.Should().Be(HttpStatusCode.OK);
        (await invalid.Content.ReadAsStringAsync()).Should().Contain("invalid");
    }

    [Fact]
    public async Task LoginPage_ShouldLinkToPasswordRecovery_AndShowTheResetSuccessMessage()
    {
        HttpClient client = WebFoundationTestHelper.CreateClient(_factory);

        HttpResponseMessage login = await client.GetAsync("/login?reset=success");

        login.StatusCode.Should().Be(HttpStatusCode.OK);
        string html = await login.Content.ReadAsStringAsync();
        html.Should().Contain("Forgot your password?");
        html.Should().Contain("Your password has been reset");
    }

    private async Task<string> RequestResetAsync(HttpClient client, string email)
    {
        string antiforgery = await WebFoundationTestHelper.GetAntiforgeryTokenAsync(client, "/forgot-password");
        HttpResponseMessage response = await WebFoundationTestHelper.PostFormAsync(
            client,
            "/auth/password-reset/request",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = antiforgery,
                ["email"] = email,
            });

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        WebFoundationTestHelper.LocationPath(response).Should().Be("/forgot-password?sent=true");

        string? token = _factory.ResetTokens.TokenFor(email);
        token.Should().NotBeNullOrWhiteSpace();
        return token!;
    }
}
