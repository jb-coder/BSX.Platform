using System.Net.Http.Json;
using System.Security.Cryptography;
using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Domain.Memberships;
using BSX.Modules.Identity.Domain.Roles;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.Modules.Identity.Infrastructure.Persistence;
using BSX.Modules.Identity.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ValueObjects = BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Web.IntegrationTests;

/// <summary>
/// Boots the real <c>BSX.Web</c> host with SQLite in-memory replacing PostgreSQL, and seeds an
/// administrator with a tenant membership.
/// </summary>
public sealed class IdentityApiFactory : WebApplicationFactory<Program>
{
    /// <summary>The seeded administrator email.</summary>
    public const string AdminEmail = "admin@example.com";

    /// <summary>The seeded administrator password.</summary>
    public const string AdminPassword = "Passw0rd!";

    /// <summary>The permissions granted to the seeded administrator.</summary>
    public static readonly string[] AdminPermissions =
    [
        "identity.users.read",
        "identity.users.create",
        "identity.roles.read",
        "identity.roles.create",
        "identity.users.assign-role",
    ];

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly RSA _rsa = RSA.Create(2048);

    /// <summary>Initializes a new instance of the <see cref="IdentityApiFactory"/> class.</summary>
    public IdentityApiFactory()
    {
        TenantId = Guid.NewGuid();
        _connection.Open();
        _ = CreateClient();
        Seed();
    }

    /// <summary>Gets the seeded tenant identifier.</summary>
    public Guid TenantId { get; }

    /// <summary>Logs in the seeded administrator and returns the access token.</summary>
    /// <param name="client">The HTTP client.</param>
    public async Task<string> LoginAsync(HttpClient client)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/identity/login",
            new { email = AdminEmail, password = AdminPassword });
        response.EnsureSuccessStatusCode();

        LoginTokens tokens = (await response.Content.ReadFromJsonAsync<LoginTokens>())!;
        return tokens.AccessToken;
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Identity:Jwt:Issuer"] = "bsx-tests",
            ["Identity:Jwt:Audience"] = "bsx-tests",
            ["Identity:Jwt:SigningKeys:0:Kid"] = "test",
            ["Identity:Jwt:SigningKeys:0:PrivateKeyPem"] = _rsa.ExportRSAPrivateKeyPem(),
        }));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<IdentityDbContext>>();
            services.RemoveAll<IdentityDbContext>();
            services.RemoveAll<IDbContextOptionsConfiguration<IdentityDbContext>>();

            services.AddDbContext<IdentityDbContext>((provider, options) =>
            {
                options.UseSqlite(_connection);
                options.AddInterceptors(provider.GetRequiredService<AuditingSaveChangesInterceptor>());
            });

            // The fail-closed limiter is real and correct; raise the thresholds so the test suite
            // (which performs many logins) does not trip it.
            services.AddSingleton(new RateLimitOptions
            {
                Login = new RateLimitPolicy(1000, TimeSpan.FromMinutes(1)),
                Refresh = new RateLimitPolicy(1000, TimeSpan.FromMinutes(1)),
                PasswordReset = new RateLimitPolicy(1000, TimeSpan.FromMinutes(1)),
            });
        });
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
            _rsa.Dispose();
        }
    }

    private void Seed()
    {
        using IServiceScope scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        context.Database.EnsureCreated();

        var now = DateTimeOffset.UtcNow;

        Role role = Role.Create(
            RoleId.New(),
            null,
            RoleName.Create("Administrator").Value,
            AdminPermissions.Select(code => Permission.Create(code).Value),
            isSystem: true).Value;
        context.Roles.Add(role);

        IPasswordHasher passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        User admin = User.Register(UserId.New(), Email.Create(AdminEmail).Value, PersonName.Create("Administrator").Value, now).Value;
        admin.SetPassword(passwordHasher.Hash(AdminPassword), now);
        admin.Activate(now);
        admin.AssignPlatformRole(role.Id);
        context.Users.Add(admin);

        TenantMembership membership = TenantMembership.Invite(MembershipId.New(), ValueObjects.TenantId.From(TenantId), admin.Id, null, now).Value;
        membership.Accept(now);
        context.TenantMemberships.Add(membership);

        context.SaveChanges();
    }

    /// <summary>The token payload returned by login/refresh.</summary>
    public sealed record LoginTokens(string AccessToken, DateTimeOffset AccessTokenExpiresAtUtc, string RefreshToken);
}
