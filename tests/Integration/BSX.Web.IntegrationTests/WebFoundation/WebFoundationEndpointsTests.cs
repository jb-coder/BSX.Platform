using System.Net;

namespace BSX.Web.IntegrationTests.WebFoundation;

public sealed class WebFoundationEndpointsTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public WebFoundationEndpointsTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_ShouldSetCookie_AndOpenTheApplicationShell()
    {
        HttpClient client = WebFoundationTestHelper.CreateClient(_factory);
        string antiforgery = await WebFoundationTestHelper.GetAntiforgeryTokenAsync(client, "/login");

        HttpResponseMessage login = await WebFoundationTestHelper.PostFormAsync(
            client,
            "/auth/login",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = antiforgery,
                ["email"] = IdentityApiFactory.AdminEmail,
                ["password"] = IdentityApiFactory.AdminPassword,
                ["returnUrl"] = "/",
            });

        login.StatusCode.Should().Be(HttpStatusCode.Redirect);
        WebFoundationTestHelper.LocationPath(login).Should().Be("/");

        HttpResponseMessage home = await client.GetAsync("/");
        home.StatusCode.Should().Be(HttpStatusCode.OK);
        string homeHtml = await home.Content.ReadAsStringAsync();
        homeHtml.Should().Contain("Welcome");
        homeHtml.Should().Contain("Workspace");
        homeHtml.Should().Contain("CRM");
        homeHtml.Should().Contain("Sales");
        homeHtml.Should().Contain("class=\"skip-link\"");
        homeHtml.Should().Contain("aria-label=\"Primary navigation\"");
        homeHtml.Should().Contain("aria-label=\"Breadcrumb\"");
        homeHtml.Should().Contain("id=\"main-content\"");

        HttpResponseMessage customers = await client.GetAsync("/app/crm/customers");
        customers.StatusCode.Should().Be(HttpStatusCode.OK);
        (await customers.Content.ReadAsStringAsync()).Should().Contain("Customer workspace is ready");
    }

    [Fact]
    public async Task Login_ShouldRedirectToWorkspaceChooser_WhenUserHasMultipleMemberships()
    {
        HttpClient client = WebFoundationTestHelper.CreateClient(_factory);
        string antiforgery = await WebFoundationTestHelper.GetAntiforgeryTokenAsync(client, "/login");

        HttpResponseMessage login = await WebFoundationTestHelper.PostFormAsync(
            client,
            "/auth/login",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = antiforgery,
                ["email"] = IdentityApiFactory.TenantAdminEmail,
                ["password"] = IdentityApiFactory.AdminPassword,
                ["returnUrl"] = "/app/sales/leads",
            });

        login.StatusCode.Should().Be(HttpStatusCode.Redirect);
        string? location = WebFoundationTestHelper.LocationPath(login);
        location.Should().StartWith("/select-tenant?token=");

        HttpResponseMessage chooser = await client.GetAsync(location);
        chooser.StatusCode.Should().Be(HttpStatusCode.OK);
        string chooserHtml = await chooser.Content.ReadAsStringAsync();
        chooserHtml.Should().Contain("Choose a workspace");
        chooserHtml.Should().Contain("Workspace");

        string selectToken = WebFoundationTestHelper.QueryValue(login, "token")!;
        string selectAntiforgery = await WebFoundationTestHelper.GetAntiforgeryTokenAsync(
            client,
            $"/select-tenant?token={selectToken}");

        HttpResponseMessage selected = await WebFoundationTestHelper.PostFormAsync(
            client,
            "/auth/select-tenant",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = selectAntiforgery,
                ["token"] = selectToken,
                ["tenantId"] = _factory.TenantId.ToString(),
            });

        selected.StatusCode.Should().Be(HttpStatusCode.Redirect);
        WebFoundationTestHelper.LocationPath(selected).Should().Be("/app/sales/leads");
    }

    [Fact]
    public async Task ProtectedPage_ShouldRedirectUnauthenticatedUsersToLogin()
    {
        HttpClient client = WebFoundationTestHelper.CreateClient(_factory);

        HttpResponseMessage response = await client.GetAsync("/app/crm/customers");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        WebFoundationTestHelper.LocationPath(response).Should().StartWith("/login?returnUrl=");
    }

    [Fact]
    public async Task AuthenticatedUserWithoutPermission_ShouldReceiveFriendlyAccessDenied()
    {
        HttpClient client = WebFoundationTestHelper.CreateClient(_factory);
        string antiforgery = await WebFoundationTestHelper.GetAntiforgeryTokenAsync(client, "/login");

        HttpResponseMessage login = await WebFoundationTestHelper.PostFormAsync(
            client,
            "/auth/login",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = antiforgery,
                ["email"] = IdentityApiFactory.RestrictedEmail,
                ["password"] = IdentityApiFactory.AdminPassword,
                ["returnUrl"] = "/",
            });

        login.StatusCode.Should().Be(HttpStatusCode.Redirect);

        HttpResponseMessage denied = await client.GetAsync("/app/crm/customers");
        denied.StatusCode.Should().Be(HttpStatusCode.Redirect);
        WebFoundationTestHelper.LocationPath(denied).Should().StartWith("/access-denied");

        HttpResponseMessage deniedPage = await client.GetAsync(WebFoundationTestHelper.LocationPath(denied));
        deniedPage.StatusCode.Should().Be(HttpStatusCode.OK);
        (await deniedPage.Content.ReadAsStringAsync()).Should().Contain("Access denied");
    }

    [Fact]
    public async Task Logout_ShouldClearTheWebSession()
    {
        HttpClient client = WebFoundationTestHelper.CreateClient(_factory);
        string loginToken = await WebFoundationTestHelper.GetAntiforgeryTokenAsync(client, "/login");

        await WebFoundationTestHelper.PostFormAsync(
            client,
            "/auth/login",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = loginToken,
                ["email"] = IdentityApiFactory.AdminEmail,
                ["password"] = IdentityApiFactory.AdminPassword,
                ["returnUrl"] = "/",
            });

        string logoutToken = await WebFoundationTestHelper.GetAntiforgeryTokenAsync(client, "/");

        HttpResponseMessage logout = await WebFoundationTestHelper.PostFormAsync(
            client,
            "/auth/logout",
            new Dictionary<string, string> { ["__RequestVerificationToken"] = logoutToken });

        logout.StatusCode.Should().Be(HttpStatusCode.Redirect);
        WebFoundationTestHelper.LocationPath(logout).Should().Be("/login?loggedOut=true");

        HttpResponseMessage protectedPage = await client.GetAsync("/app/crm/customers");
        protectedPage.StatusCode.Should().Be(HttpStatusCode.Redirect);
        WebFoundationTestHelper.LocationPath(protectedPage).Should().StartWith("/login?returnUrl=");
    }

    [Fact]
    public async Task Login_ShouldReturnFriendlyError_WhenCredentialsAreInvalid()
    {
        HttpClient client = WebFoundationTestHelper.CreateClient(_factory);
        string antiforgery = await WebFoundationTestHelper.GetAntiforgeryTokenAsync(client, "/login");

        HttpResponseMessage login = await WebFoundationTestHelper.PostFormAsync(
            client,
            "/auth/login",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = antiforgery,
                ["email"] = IdentityApiFactory.AdminEmail,
                ["password"] = "wrong-password",
                ["returnUrl"] = "/",
            });

        login.StatusCode.Should().Be(HttpStatusCode.Redirect);
        WebFoundationTestHelper.LocationPath(login).Should().Be("/login?error=invalid_credentials");
    }

    [Fact]
    public async Task LoginPage_ShouldExposeAccessibleLandmarksAndFormLabels()
    {
        HttpClient client = WebFoundationTestHelper.CreateClient(_factory);

        HttpResponseMessage response = await client.GetAsync("/login");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("lang=\"en\"");
        html.Should().Contain("class=\"skip-link\"");
        html.Should().Contain("id=\"main-content\"");
        html.Should().Contain("for=\"email\"");
        html.Should().Contain("autocomplete=\"username\"");
        html.Should().Contain("for=\"password\"");
        html.Should().Contain("autocomplete=\"current-password\"");
        html.Should().Contain("__RequestVerificationToken");
    }
}
