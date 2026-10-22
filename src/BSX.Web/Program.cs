using BSX.BuildingBlocks.DependencyInjection;
using BSX.Modules.Crm.Application.DependencyInjection;
using BSX.Modules.Crm.Endpoints;
using BSX.Modules.Crm.Endpoints.HealthChecks;
using BSX.Modules.Crm.Infrastructure.DependencyInjection;
using BSX.Modules.Identity.Application.DependencyInjection;
using BSX.Modules.Identity.Endpoints;
using BSX.Modules.Identity.Endpoints.Authentication;
using BSX.Modules.Identity.Endpoints.HealthChecks;
using BSX.Modules.Identity.Endpoints.Persistence;
using BSX.Modules.Identity.Infrastructure.DependencyInjection;
using BSX.Modules.Identity.Infrastructure.Jwt;
using BSX.Web.Components;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
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
string? identityConnectionString = builder.Configuration.GetConnectionString("Identity");

if (string.IsNullOrWhiteSpace(identityConnectionString))
{
    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            "ConnectionStrings:Identity must be configured outside the Development environment.");
    }

    identityConnectionString = "Host=localhost;Port=5432;Database=bsx_identity;Username=postgres;Password=postgres";
}

builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(identityConnectionString);
builder.Services.AddIdentityJwt(options =>
{
    builder.Configuration.GetSection(JwtOptions.SectionName).Bind(options);
    JwtOptionsConfiguration.Apply(options, builder.Environment.IsDevelopment());
});
builder.Services.AddIdentityAuthentication();
builder.Services.AddIdentityHealthChecks();

if (builder.Configuration.GetValue("Identity:Database:VerifySchema", false))
{
    builder.Services.AddIdentitySchemaHealthCheck();
}

// CRM module: application, infrastructure and readiness checks. It shares the platform database by
// default (separate `crm` schema); configure ConnectionStrings:Crm for a dedicated database.
string crmConnectionString = builder.Configuration.GetConnectionString("Crm") ?? identityConnectionString;
builder.Services.AddCrmApplication();
builder.Services.AddCrmInfrastructure(crmConnectionString);
builder.Services.AddCrmHealthChecks();

if (builder.Configuration.GetValue("Crm:Database:VerifySchema", false))
{
    builder.Services.AddCrmSchemaHealthCheck();
}

// One module-aware unit of work composes every module context (ADR-009).
builder.Services.AddCompositeUnitOfWork();

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

await app.ApplyIdentityMigrationsAsync();

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
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live"),
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
});
app.MapIdentityEndpoints();
app.MapCrmEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
