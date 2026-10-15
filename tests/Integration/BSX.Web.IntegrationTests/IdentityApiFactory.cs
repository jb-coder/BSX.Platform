using System.Net.Http.Headers;
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
/// Boots the real <c>BSX.Web</c> host with SQLite in-memory replacing PostgreSQL, and seeds a
/// multi-tenant, multi-user baseline for endpoint integration tests.
/// </summary>
public class IdentityApiFactory : WebApplicationFactory<Program>
{
    /// <summary>The seeded administrator email (member of the primary tenant only).</summary>
    public const string AdminEmail = "admin@example.com";

    /// <summary>The seeded administrator password (shared by every seeded account).</summary>
    public const string AdminPassword = "Passw0rd!";

    /// <summary>The email of a user with an active membership but no granted permissions.</summary>
    public const string RestrictedEmail = "restricted@example.com";

    /// <summary>The email of a user who belongs to both seeded tenants.</summary>
    public const string TenantAdminEmail = "tenant-admin@example.com";

    /// <summary>The email of a user with no tenant membership at all.</summary>
    public const string InviteeEmail = "invitee@example.com";

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
    private readonly CapturingPasswordResetNotifier _notifier = new();

    /// <summary>Initializes a new instance of the <see cref="IdentityApiFactory"/> class.</summary>
    public IdentityApiFactory()
    {
        TenantId = Guid.NewGuid();
        OtherTenantId = Guid.NewGuid();
        Clock = new MutableTimeProvider(DateTimeOffset.UtcNow);
        Rsa = RSA.Create(2048);
        _connection.Open();
        _ = CreateClient();
        Seed();
    }

    /// <summary>Gets the primary seeded tenant identifier.</summary>
    public Guid TenantId { get; }

    /// <summary>Gets the secondary seeded tenant identifier.</summary>
    public Guid OtherTenantId { get; }

    /// <summary>Gets the controllable clock used by the application handlers.</summary>
    public MutableTimeProvider Clock { get; }

    /// <summary>Gets the capturing password-reset notifier.</summary>
    public CapturingPasswordResetNotifier ResetTokens => _notifier;

    /// <summary>Gets the seeded administrator's user identifier.</summary>
    public Guid AdminUserId { get; private set; }

    /// <summary>Gets the restricted user's identifier.</summary>
    public Guid RestrictedUserId { get; private set; }

    /// <summary>Gets the two-tenant user's identifier.</summary>
    public Guid TenantAdminUserId { get; private set; }

    /// <summary>Gets the membership-less user's identifier.</summary>
    public Guid InviteeUserId { get; private set; }

    /// <summary>Gets the RSA key used to sign test tokens.</summary>
    protected RSA Rsa { get; }

    /// <summary>Gets the signing key PEM injected into the host configuration.</summary>
    protected virtual string SigningKeyPem => Rsa.ExportRSAPrivateKeyPem();

    /// <summary>Logs in the seeded administrator and returns the access token.</summary>
    /// <param name="client">The HTTP client.</param>
    public async Task<string> LoginAsync(HttpClient client)
    {
        LoginTokens tokens = await LoginAsync(client, AdminEmail, AdminPassword).ConfigureAwait(false);
        return tokens.AccessToken;
    }

    /// <summary>Logs in the given account, optionally selecting a tenant.</summary>
    /// <param name="client">The HTTP client.</param>
    /// <param name="email">The account email.</param>
    /// <param name="password">The account password.</param>
    /// <param name="tenantId">The tenant to authenticate against, if any.</param>
    public async Task<LoginTokens> LoginAsync(HttpClient client, string email, string password, Guid? tenantId = null)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/identity/login",
            new { email, password, tenantId }).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        LoginTokens tokens = (await response.Content.ReadFromJsonAsync<LoginTokens>().ConfigureAwait(false))!;
        return tokens;
    }

    /// <summary>Creates an HTTP client authenticated as the given account.</summary>
    /// <param name="email">The account email.</param>
    /// <param name="password">The account password.</param>
    /// <param name="tenantId">The tenant to authenticate against, if any.</param>
    public async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password, Guid? tenantId = null)
    {
        HttpClient client = CreateClient();
        LoginTokens tokens = await LoginAsync(client, email, password, tenantId).ConfigureAwait(false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return client;
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
            ["Identity:Jwt:SigningKeys:0:PrivateKeyPem"] = SigningKeyPem,
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
            services.RemoveAll<RateLimitOptions>();
            services.AddSingleton(new RateLimitOptions
            {
                Login = new RateLimitPolicy(1000, TimeSpan.FromMinutes(1)),
                Refresh = new RateLimitPolicy(1000, TimeSpan.FromMinutes(1)),
                PasswordReset = new RateLimitPolicy(1000, TimeSpan.FromMinutes(1)),
            });

            // Deterministic time and out-of-band reset-token capture for endpoint assertions.
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
            services.RemoveAll<IPasswordResetNotifier>();
            services.AddSingleton<IPasswordResetNotifier>(_notifier);
        });
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
            Rsa.Dispose();
        }
    }

    private void Seed()
    {
        using IServiceScope scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        context.Database.EnsureCreated();

        var now = DateTimeOffset.UtcNow;
        IPasswordHasher passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        Role role = Role.Create(
            RoleId.New(),
            null,
            RoleName.Create("Administrator").Value,
            AdminPermissions.Select(code => Permission.Create(code).Value),
            isSystem: true).Value;
        context.Roles.Add(role);

        AdminUserId = AddUser(context, passwordHasher, AdminEmail, "Administrator", now, platformRoleId: role.Id.Value, memberships: [TenantId]);
        RestrictedUserId = AddUser(context, passwordHasher, RestrictedEmail, "Restricted User", now, platformRoleId: null, memberships: [TenantId]);
        TenantAdminUserId = AddUser(context, passwordHasher, TenantAdminEmail, "Tenant Admin", now, platformRoleId: role.Id.Value, memberships: [TenantId, OtherTenantId]);
        InviteeUserId = AddUser(context, passwordHasher, InviteeEmail, "Invitee", now, platformRoleId: null, memberships: []);

        context.SaveChanges();
    }

    private static Guid AddUser(
        IdentityDbContext context,
        IPasswordHasher passwordHasher,
        string email,
        string name,
        DateTimeOffset now,
        Guid? platformRoleId,
        IReadOnlyCollection<Guid> memberships)
    {
        User user = User.Register(UserId.New(), Email.Create(email).Value, PersonName.Create(name).Value, now).Value;
        user.SetPassword(passwordHasher.Hash(AdminPassword), now);
        user.Activate(now);

        if (platformRoleId is { } roleId)
        {
            // A distinct RoleId instance per user; EF tracks the owned collection by reference.
            user.AssignPlatformRole(RoleId.From(roleId));
        }

        context.Users.Add(user);

        foreach (Guid tenantId in memberships)
        {
            TenantMembership membership = TenantMembership
                .Invite(MembershipId.New(), ValueObjects.TenantId.From(tenantId), user.Id, null, now)
                .Value;
            membership.Accept(now);
            context.TenantMemberships.Add(membership);
        }

        return user.Id.Value;
    }

    /// <summary>The token payload returned by login/refresh.</summary>
    public sealed record LoginTokens(string AccessToken, DateTimeOffset AccessTokenExpiresAtUtc, string RefreshToken);
}
