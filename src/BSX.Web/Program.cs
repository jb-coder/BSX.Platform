using System.Security.Cryptography;
using BSX.BuildingBlocks.DependencyInjection;
using BSX.Modules.Identity.Application.DependencyInjection;
using BSX.Modules.Identity.Endpoints;
using BSX.Modules.Identity.Endpoints.Authentication;
using BSX.Modules.Identity.Endpoints.HealthChecks;
using BSX.Modules.Identity.Infrastructure.DependencyInjection;
using BSX.Modules.Identity.Infrastructure.Jwt;
using BSX.Web.Components;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// Platform building blocks: CQRS, domain events, validation and logging behaviors.
builder.Services.AddBuildingBlocks();

// Blazor Web App with interactive server rendering.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHealthChecks();

// Identity module: application handlers, infrastructure, JWT, authentication, health checks.
string identityConnectionString = builder.Configuration.GetConnectionString("Identity")
    ?? "Host=localhost;Port=5432;Database=bsx_identity;Username=postgres;Password=postgres";

builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(identityConnectionString);
builder.Services.AddIdentityJwt(ConfigureIdentityJwt(builder.Configuration));
builder.Services.AddIdentityAuthentication();
builder.Services.AddIdentityHealthChecks();

const string ServiceName = "BSX.Platform";
string? otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(ServiceName))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();

        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            tracing.AddOtlpExporter(exporter => exporter.Endpoint = new Uri(otlpEndpoint));
        }
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();

        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            metrics.AddOtlpExporter(exporter => exporter.Endpoint = new Uri(otlpEndpoint));
        }
    });

WebApplication app = builder.Build();

app.UseSerilogRequestLogging();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapHealthChecks("/health");
app.MapIdentityEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// Loads JWT options from configuration; generates an ephemeral key in development when none is set.
static Action<JwtOptions> ConfigureIdentityJwt(IConfiguration configuration) => options =>
{
    configuration.GetSection(JwtOptions.SectionName).Bind(options);

    if (options.SigningKeys.Count == 0)
    {
        using RSA rsa = RSA.Create(2048);
        options.SigningKeys.Add(new JwtSigningKey
        {
            Kid = "development",
            PrivateKeyPem = rsa.ExportRSAPrivateKeyPem(),
        });
    }
};
