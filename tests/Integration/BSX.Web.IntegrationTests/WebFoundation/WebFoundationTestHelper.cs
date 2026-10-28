using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BSX.Web.IntegrationTests.WebFoundation;

internal static class WebFoundationTestHelper
{
    private static readonly Regex AntiforgeryPattern = new(
        "name=\"__RequestVerificationToken\"[^>]*value=\"(?<token>[^\"]+)\"",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static HttpClient CreateClient(IdentityApiFactory factory)
        => factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

    public static async Task<HttpClient> SignInAsync(
        IdentityApiFactory factory,
        string email,
        string password)
    {
        HttpClient client = CreateClient(factory);
        string antiforgery = await GetAntiforgeryTokenAsync(client, "/login");

        HttpResponseMessage login = await PostFormAsync(
            client,
            "/auth/login",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = antiforgery,
                ["email"] = email,
                ["password"] = password,
                ["returnUrl"] = "/",
            });

        login.StatusCode.Should().Be(System.Net.HttpStatusCode.Redirect);

        return client;
    }

    public static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string path)
    {
        HttpResponseMessage response = await client.GetAsync(path);
        response.StatusCode.Should().Be(HttpStatusCode.OK, "the auth page must render before posting");

        string html = await response.Content.ReadAsStringAsync();
        Match match = AntiforgeryPattern.Match(html);
        match.Success.Should().BeTrue("the form must include an antiforgery token");
        return WebUtility.HtmlDecode(match.Groups["token"].Value);
    }

    public static Task<HttpResponseMessage> PostFormAsync(
        HttpClient client,
        string path,
        IDictionary<string, string> fields)
        => client.PostAsync(path, new FormUrlEncodedContent(fields));

    public static string? LocationPath(HttpResponseMessage response)
    {
        string? location = response.Headers.Location?.OriginalString;
        if (string.IsNullOrWhiteSpace(location))
        {
            return null;
        }

        return Uri.TryCreate(location, UriKind.Absolute, out Uri? absolute)
            ? absolute.PathAndQuery
            : location;
    }

    public static string? QueryValue(HttpResponseMessage response, string key)
    {
        string? location = LocationPath(response);
        int queryStart = location?.IndexOf('?') ?? -1;
        if (location is null || queryStart < 0)
        {
            return null;
        }

        return Microsoft.AspNetCore.WebUtilities.QueryHelpers
            .ParseQuery(location[queryStart..])[key]
            .ToString();
    }
}
