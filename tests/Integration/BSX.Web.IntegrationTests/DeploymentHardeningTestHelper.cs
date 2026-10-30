using System.Net;
using System.Text.RegularExpressions;
using BSX.Web.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BSX.Web.IntegrationTests;

/// <summary>Helpers for exercising the reverse-proxy and cookie deployment surface.</summary>
internal static class DeploymentHardeningTestHelper
{
    private static readonly Regex AntiforgeryPattern = new(
        "name=\"__RequestVerificationToken\"[^>]*value=\"(?<token>[^\"]+)\"",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Creates a client that does not follow redirects.</summary>
    public static HttpClient CreateClient(WebApplicationFactory<Program> factory, bool handleCookies = false)
        => factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = handleCookies,
        });

    /// <summary>Adds trusted-proxy headers to a request.</summary>
    public static void ApplyForwardedHeaders(
        HttpRequestMessage request,
        string? proto = "https",
        string? host = "app.example.com",
        string? forIp = "203.0.113.10")
    {
        if (proto is not null)
        {
            request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", proto);
        }

        if (host is not null)
        {
            request.Headers.TryAddWithoutValidation("X-Forwarded-Host", host);
        }

        if (forIp is not null)
        {
            request.Headers.TryAddWithoutValidation("X-Forwarded-For", forIp);
        }
    }

    /// <summary>Reads the antiforgery token and cookie from the sign-in page.</summary>
    public static async Task<DeploymentAntiforgery> GetAntiforgeryAsync(
        HttpClient client,
        string path = "/login",
        string? proto = "https")
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        ApplyForwardedHeaders(request, proto);
        HttpResponseMessage response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string html = await response.Content.ReadAsStringAsync();
        Match match = AntiforgeryPattern.Match(html);
        match.Success.Should().BeTrue("the form must include an antiforgery token");
        string token = WebUtility.HtmlDecode(match.Groups["token"].Value);

        string? setCookie = GetSetCookie(response, WebAuthDefaults.AntiforgeryCookieName);
        setCookie.Should().NotBeNull("the antiforgery cookie must be issued");
        string cookie = setCookie!.Split(';', 2)[0];

        return new DeploymentAntiforgery(token, cookie, setCookie);
    }

    /// <summary>Returns the first <c>Set-Cookie</c> value for the named cookie.</summary>
    public static string? GetSetCookie(HttpResponseMessage response, string cookieName)
        => response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? values)
            ? values.FirstOrDefault(value =>
                value.StartsWith(cookieName + "=", StringComparison.OrdinalIgnoreCase))
            : null;

    /// <summary>Determines whether a <c>Set-Cookie</c> value carries the given attribute.</summary>
    public static bool HasCookieAttribute(string setCookie, string attribute)
        => setCookie
            .Split(';')
            .Skip(1)
            .Any(part => string.Equals(part.Trim(), attribute, StringComparison.OrdinalIgnoreCase));

    /// <summary>Posts the sign-in form with the antiforgery cookie and forwarded headers.</summary>
    public static async Task<HttpResponseMessage> PostLoginAsync(
        HttpClient client,
        string email,
        string password,
        DeploymentAntiforgery antiforgery,
        string? proto = "https")
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/auth/login");
        ApplyForwardedHeaders(request, proto);
        request.Headers.TryAddWithoutValidation("Cookie", antiforgery.Cookie);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = antiforgery.Token,
            ["email"] = email,
            ["password"] = password,
            ["returnUrl"] = "/",
        });

        return await client.SendAsync(request);
    }

    internal sealed record DeploymentAntiforgery(string Token, string Cookie, string SetCookieHeader);
}
