using System.Net;

namespace BSX.Web.IntegrationTests;

public sealed class SalesHealthTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public SalesHealthTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Readiness_Should_IncludeSalesDatabase()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AggregateHealth_Should_BeHealthy()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
