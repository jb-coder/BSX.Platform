using System.Net;
using BSX.Modules.Identity.Domain.Sessions;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.Modules.Identity.Infrastructure.Persistence;
using BSX.Web.Authentication;
using BSX.Web.Deployment;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace BSX.Web.IntegrationTests;

/// <summary>
/// Deployment hardening coverage: forwarded headers, reverse-proxy HTTPS detection, secure cookies,
/// Data Protection persistence and startup configuration validation.
/// </summary>
public sealed class DeploymentHardeningTests :
    IClassFixture<IdentityApiFactory>,
    IClassFixture<UntrustedProxyIdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;
    private readonly UntrustedProxyIdentityApiFactory _untrustedProxyFactory;

    public DeploymentHardeningTests(
        IdentityApiFactory factory,
        UntrustedProxyIdentityApiFactory untrustedProxyFactory)
    {
        _factory = factory;
        _untrustedProxyFactory = untrustedProxyFactory;
    }

    [Fact]
    public async Task ForwardedProto_ShouldPreventTheHttpsRedirectLoop()
    {
        using var factory = CreateHttpsRedirectFactory();
        using HttpClient client = DeploymentHardeningTestHelper.CreateClient(factory);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        DeploymentHardeningTestHelper.ApplyForwardedHeaders(request, proto: "https");

        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MissingForwardedProto_ShouldRedirectToHttpsUsingTheForwardedHost()
    {
        using var factory = CreateHttpsRedirectFactory();
        using HttpClient client = DeploymentHardeningTestHelper.CreateClient(factory);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        DeploymentHardeningTestHelper.ApplyForwardedHeaders(request, proto: "http", host: "app.example.com");

        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.TemporaryRedirect);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.Scheme.Should().Be("https");
        response.Headers.Location.Host.Should().Be("app.example.com");
        response.Headers.Location.AbsolutePath.Should().Be("/health/live");
    }

    [Fact]
    public async Task UntrustedProxy_ShouldNotBeAbleToSpoofTheScheme()
    {
        using HttpClient client = DeploymentHardeningTestHelper.CreateClient(_untrustedProxyFactory);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        DeploymentHardeningTestHelper.ApplyForwardedHeaders(request, proto: "https");

        HttpResponseMessage response = await client.SendAsync(request);

        // The loopback test client is not the configured trusted proxy, so the forwarded scheme is
        // ignored and the request is still treated as plain HTTP.
        response.StatusCode.Should().Be(HttpStatusCode.TemporaryRedirect);
    }

    [Fact]
    public async Task LoginBehindTlsTermination_ShouldIssueSecureHttpOnlyCookies()
    {
        using HttpClient client = DeploymentHardeningTestHelper.CreateClient(_factory);
        DeploymentHardeningTestHelper.DeploymentAntiforgery antiforgery =
            await DeploymentHardeningTestHelper.GetAntiforgeryAsync(client);

        DeploymentHardeningTestHelper.HasCookieAttribute(antiforgery.SetCookieHeader, "Secure").Should().BeTrue();
        DeploymentHardeningTestHelper.HasCookieAttribute(antiforgery.SetCookieHeader, "HttpOnly").Should().BeTrue();
        DeploymentHardeningTestHelper.HasCookieAttribute(antiforgery.SetCookieHeader, "SameSite=Strict").Should().BeTrue();

        HttpResponseMessage login = await DeploymentHardeningTestHelper.PostLoginAsync(
            client,
            IdentityApiFactory.AdminEmail,
            IdentityApiFactory.AdminPassword,
            antiforgery);

        login.StatusCode.Should().Be(HttpStatusCode.Redirect);

        string? authCookie = DeploymentHardeningTestHelper.GetSetCookie(login, WebAuthDefaults.AuthCookieName);
        authCookie.Should().NotBeNull("the authentication cookie must be issued");
        DeploymentHardeningTestHelper.HasCookieAttribute(authCookie!, "Secure").Should().BeTrue();
        DeploymentHardeningTestHelper.HasCookieAttribute(authCookie!, "HttpOnly").Should().BeTrue();
        DeploymentHardeningTestHelper.HasCookieAttribute(authCookie!, "SameSite=Lax").Should().BeTrue();

        string? refreshCookie = DeploymentHardeningTestHelper.GetSetCookie(
            login,
            WebAuthDefaults.RefreshTokenCookieName);
        refreshCookie.Should().NotBeNull("the refresh-token cookie must be issued");
        DeploymentHardeningTestHelper.HasCookieAttribute(refreshCookie!, "Secure").Should().BeTrue();
        DeploymentHardeningTestHelper.HasCookieAttribute(refreshCookie!, "HttpOnly").Should().BeTrue();
        DeploymentHardeningTestHelper.HasCookieAttribute(refreshCookie!, "SameSite=Strict").Should().BeTrue();
    }

    [Fact]
    public async Task ForwardedFor_ShouldBeUsedForTheSessionClientIp()
    {
        using HttpClient client = DeploymentHardeningTestHelper.CreateClient(_factory);
        DeploymentHardeningTestHelper.DeploymentAntiforgery antiforgery =
            await DeploymentHardeningTestHelper.GetAntiforgeryAsync(client);

        HttpResponseMessage login = await DeploymentHardeningTestHelper.PostLoginAsync(
            client,
            IdentityApiFactory.AdminEmail,
            IdentityApiFactory.AdminPassword,
            antiforgery);
        login.StatusCode.Should().Be(HttpStatusCode.Redirect);

        await using AsyncServiceScope scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        List<UserSession> sessions = await context.Sessions
            .AsNoTracking()
            .Where(candidate => candidate.UserId == UserId.From(_factory.AdminUserId))
            .ToListAsync();
        UserSession session = sessions.OrderByDescending(candidate => candidate.CreatedOnUtc).First();

        session.Metadata.IpAddress.Should().Be("203.0.113.10");
    }

    [Fact]
    public async Task SecurityHeaders_ShouldBePresentOnResponses()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync("/login");

        response.Headers.GetValues("X-Content-Type-Options").Should().ContainSingle().Which.Should().Be("nosniff");
        response.Headers.GetValues("X-Frame-Options").Should().ContainSingle().Which.Should().Be("DENY");
        response.Headers.GetValues("Referrer-Policy").Should().ContainSingle().Which.Should().Be("no-referrer");
        response.Headers.GetValues("Content-Security-Policy").Should()
            .Contain(value => value.Contains("frame-ancestors 'none'", StringComparison.Ordinal));
        response.Headers.GetValues("Permissions-Policy").Should().ContainSingle();
    }

    [Fact]
    public async Task DataProtection_ShouldPersistKeysToTheConfiguredDirectory()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        _factory.Services.GetRequiredService<IConfiguration>()["DataProtection:KeysPath"]
            .Should().Be(_factory.DataProtectionKeysPath);
        var provider = scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>();
        var keyManager = scope.ServiceProvider.GetRequiredService<IKeyManager>();

        string payload = provider.CreateProtector("persistence").Protect("round-trip");
        IKey newestKey = keyManager.GetAllKeys().OrderByDescending(key => key.CreationDate).First();
        await WaitForFileAsync(
            Path.Combine(_factory.DataProtectionKeysPath, $"key-{newestKey.KeyId}.xml"));

        IDataProtectionProvider restarted = DataProtectionProvider.Create(
            new DirectoryInfo(_factory.DataProtectionKeysPath),
            builder => builder.SetApplicationName("BSX.Platform.Tests"));

        restarted.CreateProtector("persistence").Unprotect(payload).Should().Be("round-trip");
    }

    [Fact]
    public void DataProtection_ShouldRequireAPersistentPathOutsideDevelopment()
    {
        var environment = new TestHostEnvironment { EnvironmentName = Environments.Production };

        Action act = () => WebDeploymentHardeningExtensions.ResolveDataProtectionKeysPath(
            new BSX.Web.Deployment.DataProtectionOptions(),
            environment);

        act.Should().Throw<InvalidOperationException>().WithMessage("*DataProtection:KeysPath*");
    }

    [Fact]
    public void DataProtection_ShouldRejectANonWritablePath()
    {
        string filePath = Path.Combine(Path.GetTempPath(), $"bsx-dp-file-{Guid.NewGuid():N}");
        File.WriteAllText(filePath, "not-a-directory");

        try
        {
            var environment = new TestHostEnvironment { ContentRootPath = Path.GetTempPath() };

            Action act = () => WebDeploymentHardeningExtensions.ResolveDataProtectionKeysPath(
                new BSX.Web.Deployment.DataProtectionOptions { KeysPath = filePath },
                environment);

            act.Should().Throw<InvalidOperationException>().WithMessage("*not a writable directory*");
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Startup_ShouldRejectWildcardAllowedHostsOutsideDevelopment()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["AllowedHosts"] = "*" })
            .Build();
        var environment = new TestHostEnvironment { EnvironmentName = Environments.Production };

        Action act = () => WebDeploymentHardeningExtensions.ValidateAllowedHosts(configuration, environment);

        act.Should().Throw<InvalidOperationException>().WithMessage("*AllowedHosts*");
    }

    private WebApplicationFactory<Program> CreateHttpsRedirectFactory()
        => _factory.WithWebHostBuilder(builder => builder.ConfigureServices(
            services => services.Configure<HttpsRedirectionOptions>(options => options.HttpsPort = 443)));

    private static async Task WaitForFileAsync(string path)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(10);

        while (!File.Exists(path))
        {
            if (DateTimeOffset.UtcNow > deadline)
            {
                string directory = Path.GetDirectoryName(path) ?? string.Empty;
                string existing = Directory.Exists(directory)
                    ? string.Join(", ", Directory.GetFiles(directory))
                    : "directory does not exist";

                throw new TimeoutException(
                    $"The Data Protection key file '{path}' was not persisted. Existing files: {existing}");
            }

            await Task.Delay(50);
        }
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "BSX.Tests";

        public string ContentRootPath { get; set; } = Path.GetTempPath();

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
