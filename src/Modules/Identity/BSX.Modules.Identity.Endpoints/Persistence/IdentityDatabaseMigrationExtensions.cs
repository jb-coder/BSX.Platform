using BSX.Modules.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BSX.Modules.Identity.Endpoints.Persistence;

/// <summary>
/// Optional startup migration hook (ADR-004 deployment guidance). Migrations are normally applied
/// out of band by the deployment pipeline; set <c>Identity:Database:ApplyMigrationsOnStartup</c> to
/// <c>true</c> to apply them on boot (single-instance deployments only).
/// </summary>
public static class IdentityDatabaseMigrationExtensions
{
    /// <summary>Applies pending identity migrations when enabled by configuration.</summary>
    /// <param name="app">The application.</param>
    public static async Task ApplyIdentityMigrationsAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.Configuration.GetValue("Identity:Database:ApplyMigrationsOnStartup", false))
        {
            return;
        }

        using IServiceScope scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await context.Database.MigrateAsync().ConfigureAwait(false);
    }
}
