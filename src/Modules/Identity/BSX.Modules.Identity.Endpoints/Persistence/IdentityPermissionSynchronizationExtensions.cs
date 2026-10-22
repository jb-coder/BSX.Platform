using System.Data.Common;
using BSX.Modules.Identity.Infrastructure.Permissions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BSX.Modules.Identity.Endpoints.Persistence;

/// <summary>
/// Optional startup permission synchronization (ADR-018). It grants every registered module
/// catalog to the <c>Administrator</c> system role and every read permission to <c>Auditor</c>.
/// </summary>
public static class IdentityPermissionSynchronizationExtensions
{
    /// <summary>
    /// Synchronizes the system roles when <c>Identity:Permissions:SynchronizeOnStartup</c> is
    /// enabled. A missing schema is logged and skipped so the host can still start before the
    /// deployment pipeline applies migrations.
    /// </summary>
    /// <param name="app">The application.</param>
    public static async Task SynchronizeIdentityPermissionsAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.Configuration.GetValue("Identity:Permissions:SynchronizeOnStartup", true))
        {
            return;
        }

        using IServiceScope scope = app.Services.CreateScope();
        var synchronizer = scope.ServiceProvider.GetRequiredService<SystemRolePermissionSynchronizer>();
        ILoggerFactory loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        ILogger logger = loggerFactory.CreateLogger(typeof(IdentityPermissionSynchronizationExtensions));

        try
        {
            PermissionSynchronizationResult result = await synchronizer.SynchronizeAsync().ConfigureAwait(false);
            logger.LogInformation(
                "Permission synchronization completed: {CatalogCount} catalog permission(s), {Added} added, {Removed} removed across {RoleCount} system role(s).",
                result.CatalogCount,
                result.Added,
                result.Removed,
                result.SystemRoleCount);
        }
        catch (DbException exception)
        {
            logger.LogWarning(
                exception,
                "System role permission synchronization was skipped because the Identity schema is not available.");
        }
    }
}
