using System.Data.Common;
using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Domain.Memberships;
using BSX.Modules.Identity.Domain.Roles;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.Modules.Identity.Infrastructure.Persistence;
using BSX.Modules.Identity.Infrastructure.Permissions;
using Microsoft.EntityFrameworkCore;

namespace BSX.Web.Demo;

/// <summary>
/// Development-only demo identity bootstrap. It creates a single administrator in a stable demo
/// tenant after the Identity schema and system roles exist, so a developer can sign in to the local
/// application and exercise the web workflows.
/// </summary>
internal static class DemoIdentitySeeder
{
    /// <summary>The stable demo tenant identifier.</summary>
    public static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>The demo administrator email.</summary>
    public const string Email = "admin@bsx.local";

    /// <summary>The demo administrator password.</summary>
    public const string Password = "Passw0rd!";

    private const string DemoName = "Demo Administrator";

    /// <summary>Seeds the demo administrator when enabled and safe to do so.</summary>
    /// <param name="app">The application.</param>
    public static async Task SeedAsync(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.Environment.IsDevelopment()
            || !app.Configuration.GetValue("Demo:SeedIdentity", false))
        {
            return;
        }

        using IServiceScope scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        ILogger logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(DemoIdentitySeeder));

        try
        {
            List<Role> systemRoles = await context.Roles
                .Where(role => role.TenantId == null)
                .ToListAsync()
                .ConfigureAwait(false);

            Role? administrator = systemRoles.FirstOrDefault(role =>
                string.Equals(role.Name.Value, SystemRolePermissionSynchronizer.AdministratorRoleName, StringComparison.Ordinal));

            if (administrator is null)
            {
                logger.LogWarning(
                    "Demo identity seeding skipped: the Administrator system role does not exist. Enable permission synchronization and ensure the Identity schema is migrated.");
                return;
            }

            User? user = await context.Users
                .FirstOrDefaultAsync(candidate =>
                    EF.Property<string>(candidate, "SearchEmail") == Email)
                .ConfigureAwait(false);

            DateTimeOffset now = timeProvider.GetUtcNow();

            if (user is null)
            {
                user = User.Register(
                    UserId.New(),
                    Modules.Identity.Domain.ValueObjects.Email.Create(Email).Value,
                    PersonName.Create(DemoName).Value,
                    now).Value;

                user.SetPassword(passwordHasher.Hash(Password), now);
                user.Activate(now);
                user.AssignPlatformRole(administrator.Id);
                context.Users.Add(user);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }
            else if (!user.PlatformRoleIds.Any(roleId => roleId.Value == administrator.Id.Value))
            {
                user.AssignPlatformRole(administrator.Id);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }

            List<TenantMembership> memberships = await context.TenantMemberships
                .Where(membership => membership.UserId == user.Id)
                .ToListAsync()
                .ConfigureAwait(false);

            bool hasMembership = memberships.Any(membership => membership.TenantId.Value == TenantId);

            if (!hasMembership)
            {
                TenantMembership membership = TenantMembership
                    .Invite(
                        MembershipId.New(),
                        Modules.Identity.Domain.ValueObjects.TenantId.From(TenantId),
                        user.Id,
                        invitedByUserId: null,
                        now)
                    .Value;

                membership.Accept(now);
                context.TenantMemberships.Add(membership);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }

            logger.LogInformation(
                "Demo identity is available for {Email} in tenant {TenantId}.",
                Email,
                TenantId);
        }
        catch (Exception exception) when (exception is DbException or InvalidOperationException)
        {
            logger.LogWarning(
                exception,
                "Demo identity seeding was skipped. Apply the Identity and CRM migrations before enabling demo seeding.");
        }
    }
}
