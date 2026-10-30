using Microsoft.Extensions.Options;

namespace BSX.Web.Deployment;

/// <summary>Adds the platform security response headers to every response.</summary>
internal static class SecurityHeadersMiddleware
{
    /// <summary>Adds the configured security headers without overwriting existing values.</summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The same builder for chaining.</returns>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        SecurityHeadersOptions options = app.ApplicationServices
            .GetRequiredService<IOptions<SecurityHeadersOptions>>().Value;

        return app.Use(async (context, next) =>
        {
            IHeaderDictionary headers = context.Response.Headers;

            Add(headers, "X-Content-Type-Options", "nosniff");

            if (options.EnableFrameOptions)
            {
                Add(headers, "X-Frame-Options", "DENY");
            }

            Add(headers, "Referrer-Policy", options.ReferrerPolicy);
            Add(headers, "Permissions-Policy", options.PermissionsPolicy);

            if (options.EnableCrossOriginOpenerPolicy)
            {
                Add(headers, "Cross-Origin-Opener-Policy", "same-origin");
            }

            if (options.EnableCrossOriginResourcePolicy)
            {
                Add(headers, "Cross-Origin-Resource-Policy", "same-origin");
            }

            if (!string.IsNullOrWhiteSpace(options.ContentSecurityPolicy))
            {
                Add(headers, "Content-Security-Policy", options.ContentSecurityPolicy);
            }

            await next().ConfigureAwait(false);
        });
    }

    private static void Add(IHeaderDictionary headers, string name, string value)
    {
        if (!headers.ContainsKey(name))
        {
            headers[name] = value;
        }
    }
}
