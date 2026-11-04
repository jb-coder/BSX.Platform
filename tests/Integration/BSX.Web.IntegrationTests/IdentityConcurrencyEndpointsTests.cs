using System.Net;
using System.Net.Http.Json;
using BSX.Modules.Identity.Application.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace BSX.Web.IntegrationTests;

/// <summary>Identity optimistic-concurrency failures must map to the platform 409 problem contract.</summary>
public sealed class IdentityConcurrencyEndpointsTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public IdentityConcurrencyEndpointsTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task RevokeSession_Should_ReturnConflict_WhenConcurrentModificationIsDetected()
    {
        HttpClient client = await _factory.CreateAuthenticatedClientAsync(
            IdentityApiFactory.AdminEmail,
            IdentityApiFactory.AdminPassword);

        HttpResponseMessage list = await client.GetAsync("/identity/sessions");
        list.StatusCode.Should().Be(HttpStatusCode.OK);

        List<SessionDto>? sessions = await list.Content.ReadFromJsonAsync<List<SessionDto>>();
        sessions.Should().NotBeNullOrEmpty();

        SessionDto session = sessions![0];
        ForcedIdentityConcurrencySaveChangesInterceptor interceptor =
            _factory.Services.GetRequiredService<ForcedIdentityConcurrencySaveChangesInterceptor>();

        try
        {
            interceptor.ForceSessionConflict(
                session.Id,
                ForcedIdentityConcurrencySaveChangesInterceptor.SessionConflictMode.Update);

            HttpResponseMessage response = await client.DeleteAsync($"/identity/sessions/{session.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

            ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            problem!.Title.Should().Be("Identity.Concurrency.Conflict");
            problem.Status.Should().Be(409);
            problem.Detail.Should().NotBeNullOrWhiteSpace();
        }
        finally
        {
            interceptor.Clear();
        }

        HttpResponseMessage after = await client.GetAsync("/identity/sessions");
        after.StatusCode.Should().Be(HttpStatusCode.OK);

        List<SessionDto>? remaining = await after.Content.ReadFromJsonAsync<List<SessionDto>>();
        remaining.Should().ContainSingle(item => item.Id == session.Id && item.Status == "Active");
    }
}
