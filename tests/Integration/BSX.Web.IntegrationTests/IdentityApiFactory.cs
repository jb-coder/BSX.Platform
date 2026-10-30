using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using BSX.BuildingBlocks.Authorization;
using BSX.Modules.Crm.Infrastructure.Persistence;
using BSX.Modules.Crm.Infrastructure.Persistence.Interceptors;
using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Domain.Memberships;
using BSX.Modules.Identity.Domain.Roles;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.Modules.Identity.Infrastructure.Persistence;
using BSX.Modules.Identity.Infrastructure.Persistence.Interceptors;
using BSX.Modules.Sales.Infrastructure.Persistence;
using BSX.Modules.Sales.Infrastructure.Persistence.Interceptors;
using BSX.Persistence.Outbox;
using BSX.Web.Authentication;
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
/// Boots the real <c>BSX.Web</c> host with SQLite in-memory replacing PostgreSQL for both the
/// Identity and CRM modules, and seeds a multi-tenant, multi-user baseline for endpoint tests.
/// </summary>
public class IdentityApiFactory : WebApplicationFactory<Program>
{
    /// <summary>The seeded administrator email (member of the primary tenant only).</summary>
    public const string AdminEmail = "admin@example.com";

    /// <summary>The seeded administrator password (shared by every seeded account).</summary>
    public const string AdminPassword = "Passw0rd!";

    /// <summary>The email of a user with an active membership but no granted permissions.</summary>
    public const string RestrictedEmail = "restricted@example.com";

    /// <summary>The email of a user with read-only CRM permissions.</summary>
    public const string CrmReaderEmail = "crm-reader@example.com";

    /// <summary>The email of a user with read-only Sales and CRM permissions.</summary>
    public const string SalesReaderEmail = "sales-reader@example.com";

    /// <summary>The email of a user with read-only administration permissions.</summary>
    public const string AdministrationReaderEmail = "admin-reader@example.com";

    /// <summary>The email of a user who belongs to both seeded tenants.</summary>
    public const string TenantAdminEmail = "tenant-admin@example.com";

    /// <summary>The email of a tenant administrator who holds only workspace-role permissions.</summary>
    public const string WorkspaceAdminEmail = "workspace-admin@example.com";

    /// <summary>The email of a user with no tenant membership at all.</summary>
    public const string InviteeEmail = "invitee@example.com";

    /// <summary>The permissions granted to the seeded administrator.</summary>
    public static readonly string[] AdminPermissions =
    [
        "identity.users.read",
        "identity.users.create",
        "identity.users.update",
        "identity.users.manage-status",
        "identity.roles.read",
        "identity.roles.create",
        "identity.roles.update",
        "identity.roles.set-permissions",
        "identity.permissions.read",
        "identity.memberships.read",
        "identity.memberships.manage",
        "identity.users.assign-role",
        "crm.customers.read",
        "crm.customers.create",
        "crm.customers.update",
        "crm.organizations.read",
        "crm.organizations.create",
        "crm.organizations.update",
        "sales.leads.read",
        "sales.leads.create",
        "sales.leads.update",
        "sales.opportunities.read",
        "sales.opportunities.create",
        "sales.opportunities.update",
    ];

    /// <summary>The read-only CRM permissions granted to the seeded CRM reader.</summary>
    public static readonly string[] CrmReaderPermissions =
    [
        "crm.customers.read",
        "crm.organizations.read",
    ];

    /// <summary>The read-only Sales and CRM permissions granted to the seeded Sales reader.</summary>
    public static readonly string[] SalesReaderPermissions =
    [
        "crm.customers.read",
        "crm.organizations.read",
        "sales.leads.read",
        "sales.opportunities.read",
    ];

    /// <summary>The read-only administration permissions granted to the seeded administration reader.</summary>
    public static readonly string[] AdministrationReaderPermissions =
    [
        "identity.users.read",
        "identity.roles.read",
        "identity.memberships.read",
        "identity.permissions.read",
    ];

    /// <summary>
    /// The tenant-scoped permissions granted to the seeded workspace administrator. The account has
    /// no platform role, so it exercises the tenant-role authorization boundary.
    /// </summary>
    public static readonly string[] WorkspaceAdminPermissions =
    [
        "identity.roles.read",
        "identity.roles.create",
        "identity.roles.update",
        "identity.roles.set-permissions",
        "identity.permissions.read",
        "identity.memberships.read",
        "identity.memberships.manage",
        "crm.customers.read",
        "sales.leads.read",
    ];

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly SqliteConnection _crmConnection = new("DataSource=:memory:");
    private readonly SqliteConnection _salesConnection = new("DataSource=:memory:");
    private readonly CapturingPasswordResetNotifier _notifier = new();

    /// <summary>Initializes a new instance of the <see cref="IdentityApiFactory"/> class.</summary>
    public IdentityApiFactory()
    {
        TenantId = Guid.NewGuid();
        OtherTenantId = Guid.NewGuid();
        Clock = new MutableTimeProvider(DateTimeOffset.UtcNow);
        Rsa = RSA.Create(2048);
        _connection.Open();
        _crmConnection.Open();
        _salesConnection.Open();
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

    /// <summary>Gets the read-only CRM user's identifier.</summary>
    public Guid CrmReaderUserId { get; private set; }

    /// <summary>Gets the read-only Sales user's identifier.</summary>
    public Guid SalesReaderUserId { get; private set; }

    /// <summary>Gets the read-only administration user's identifier.</summary>
    public Guid AdministrationReaderUserId { get; private set; }

    /// <summary>Gets the two-tenant user's identifier.</summary>
    public Guid TenantAdminUserId { get; private set; }

    /// <summary>Gets the tenant-only workspace administrator's identifier.</summary>
    public Guid WorkspaceAdminUserId { get; private set; }

    /// <summary>Gets the workspace administrator role identifier.</summary>
    public Guid WorkspaceAdminRoleId { get; private set; }

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
            // The test factory seeds its own system role after the schema exists.
            ["Identity:Permissions:SynchronizeOnStartup"] = "false",
            // Outbox rows are still written so tests can assert durability; the recovery loop is
            // driven explicitly by outbox tests instead of racing the request pipeline.
            ["Outbox:ProcessingEnabled"] = "false",
        }));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<IdentityDbContext>>();
            services.RemoveAll<IdentityDbContext>();
            services.RemoveAll<IDbContextOptionsConfiguration<IdentityDbContext>>();

            services.AddDbContext<IdentityDbContext>((provider, options) =>
            {
                options.UseSqlite(_connection);
                options.AddInterceptors(
                    provider.GetRequiredService<AuditingSaveChangesInterceptor>(),
                    provider.GetRequiredService<OutboxSaveChangesInterceptor>());
            });

            services.RemoveAll<DbContextOptions<CrmDbContext>>();
            services.RemoveAll<CrmDbContext>();
            services.RemoveAll<IDbContextOptionsConfiguration<CrmDbContext>>();

            services.AddDbContext<CrmDbContext>((provider, options) =>
            {
                options.UseSqlite(_crmConnection);
                options.AddInterceptors(
                    provider.GetRequiredService<CrmAuditingSaveChangesInterceptor>(),
                    provider.GetRequiredService<OutboxSaveChangesInterceptor>(),
                    new ForcedConcurrencySaveChangesInterceptor());
            });

            services.RemoveAll<DbContextOptions<SalesDbContext>>();
            services.RemoveAll<SalesDbContext>();
            services.RemoveAll<IDbContextOptionsConfiguration<SalesDbContext>>();

            services.AddDbContext<SalesDbContext>((provider, options) =>
            {
                options.UseSqlite(_salesConnection);
                options.AddInterceptors(
                    provider.GetRequiredService<SalesAuditingSaveChangesInterceptor>(),
                    provider.GetRequiredService<CrmReferenceValidationInterceptor>(),
                    provider.GetRequiredService<OutboxSaveChangesInterceptor>(),
                    new ForcedSalesConcurrencySaveChangesInterceptor());
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

            // Revalidate on every cookie request so revocation tests are deterministic.
            services.Configure<AuthorizationRevalidationOptions>(options => options.CookieIntervalSeconds = 0);

            // Keep legacy claim-only test principals working while exercising the production
            // revalidation path for real sessions.
            services.RemoveAll<IAuthorizationRevalidator>();
            services.AddScoped<IAuthorizationRevalidator, TestAuthorizationRevalidator>();
        });
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
            _crmConnection.Dispose();
            _salesConnection.Dispose();
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

        Role crmReaderRole = Role.Create(
            RoleId.New(),
            null,
            RoleName.Create("CRM Reader").Value,
            CrmReaderPermissions.Select(code => Permission.Create(code).Value),
            isSystem: true).Value;
        context.Roles.Add(crmReaderRole);

        AdminUserId = AddUser(context, passwordHasher, AdminEmail, "Administrator", now, platformRoleId: role.Id.Value, memberships: [TenantId]);
        RestrictedUserId = AddUser(context, passwordHasher, RestrictedEmail, "Restricted User", now, platformRoleId: null, memberships: [TenantId]);
        CrmReaderUserId = AddUser(context, passwordHasher, CrmReaderEmail, "CRM Reader", now, platformRoleId: crmReaderRole.Id.Value, memberships: [TenantId]);

        Role salesReaderRole = Role.Create(
            RoleId.New(),
            null,
            RoleName.Create("Sales Reader").Value,
            SalesReaderPermissions.Select(code => Permission.Create(code).Value),
            isSystem: true).Value;
        context.Roles.Add(salesReaderRole);

        SalesReaderUserId = AddUser(context, passwordHasher, SalesReaderEmail, "Sales Reader", now, platformRoleId: salesReaderRole.Id.Value, memberships: [TenantId]);

        Role adminReaderRole = Role.Create(
            RoleId.New(),
            null,
            RoleName.Create("Administration Reader").Value,
            AdministrationReaderPermissions.Select(code => Permission.Create(code).Value),
            isSystem: true).Value;
        context.Roles.Add(adminReaderRole);

        AdministrationReaderUserId = AddUser(context, passwordHasher, AdministrationReaderEmail, "Administration Reader", now, platformRoleId: adminReaderRole.Id.Value, memberships: [TenantId]);
        TenantAdminUserId = AddUser(context, passwordHasher, TenantAdminEmail, "Tenant Admin", now, platformRoleId: role.Id.Value, memberships: [TenantId, OtherTenantId]);

        Role workspaceAdminRole = Role.Create(
            RoleId.New(),
            ValueObjects.TenantId.From(TenantId),
            RoleName.Create("Workspace Administrator").Value,
            WorkspaceAdminPermissions.Select(code => Permission.Create(code).Value)).Value;
        context.Roles.Add(workspaceAdminRole);
        WorkspaceAdminRoleId = workspaceAdminRole.Id.Value;

        WorkspaceAdminUserId = AddUser(
            context,
            passwordHasher,
            WorkspaceAdminEmail,
            "Workspace Admin",
            now,
            platformRoleId: null,
            memberships: [TenantId],
            membershipRoleId: workspaceAdminRole.Id.Value);

        InviteeUserId = AddUser(context, passwordHasher, InviteeEmail, "Invitee", now, platformRoleId: null, memberships: []);

        context.SaveChanges();

        using IServiceScope crmScope = Services.CreateScope();
        var crmContext = crmScope.ServiceProvider.GetRequiredService<CrmDbContext>();
        crmContext.Database.EnsureCreated();

        using IServiceScope salesScope = Services.CreateScope();
        var salesContext = salesScope.ServiceProvider.GetRequiredService<SalesDbContext>();
        salesContext.Database.EnsureCreated();
    }

    private static Guid AddUser(
        IdentityDbContext context,
        IPasswordHasher passwordHasher,
        string email,
        string name,
        DateTimeOffset now,
        Guid? platformRoleId,
        IReadOnlyCollection<Guid> memberships,
        Guid? membershipRoleId = null)
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

            if (membershipRoleId is { } assignedRoleId)
            {
                membership.AssignRole(RoleId.From(assignedRoleId));
            }

            context.TenantMemberships.Add(membership);
        }

        return user.Id.Value;
    }

    /// <summary>The token payload returned by login/refresh.</summary>
    public sealed record LoginTokens(string AccessToken, DateTimeOffset AccessTokenExpiresAtUtc, string RefreshToken);
}
