using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace BSX.Web.IntegrationTests;

/// <summary>
/// Ensures the in-memory test server has a loopback client address so forwarded-header processing
/// can be exercised deterministically. Harmless for every other test.
/// </summary>
internal sealed class LoopbackRemoteIpStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                context.Connection.RemoteIpAddress ??= IPAddress.Loopback;
                return nextMiddleware();
            });

            next(app);
        };
}
