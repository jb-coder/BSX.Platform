using System.Data.Common;
using System.Security.Claims;
using BSX.BuildingBlocks.Cqrs;
using BSX.Contracts.Permissions;
using BSX.Modules.Crm.Application.Addresses.AddAddress;
using BSX.Modules.Crm.Application.Common;
using BSX.Modules.Crm.Application.Contacts.AddContact;
using BSX.Modules.Crm.Application.Customers.AssignOrganization;
using BSX.Modules.Crm.Application.Customers.CreateCustomer;
using BSX.Modules.Crm.Application.Customers.ListCustomers;
using BSX.Modules.Crm.Application.Dtos;
using BSX.Modules.Crm.Application.Organizations.CreateOrganization;
using BSX.Modules.Crm.Application.Organizations.ListOrganizations;
using BSX.Modules.Identity.Application.Users.ActivateUser;
using BSX.Modules.Identity.Application.Users.RegisterUser;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Infrastructure.Persistence;
using BSX.Modules.Sales.Application.Dtos;
using BSX.Modules.Sales.Application.Leads.CreateLead;
using BSX.Modules.Sales.Application.Leads.GetLeadById;
using BSX.Modules.Sales.Application.Leads.ListLeads;
using BSX.Modules.Sales.Application.Leads.MarkLeadContacted;
using BSX.Modules.Sales.Application.Leads.QualifyLead;
using BSX.Modules.Sales.Application.Opportunities.AdvanceOpportunityStage;
using BSX.Modules.Sales.Application.Opportunities.CreateOpportunityFromLead;
using BSX.Modules.Sales.Application.Opportunities.ListOpportunities;
using BSX.SharedKernel.Results;
using BSX.Web.Authentication;
using Microsoft.EntityFrameworkCore;

namespace BSX.Web.Demo;

/// <summary>
/// Development-only sample workspace data for customer demonstrations and pilot walkthroughs. The
/// seeder is opt-in, idempotent and uses the application commands so the seeded records follow the
/// same domain and authorization rules as records created through the UI.
/// </summary>
internal static class DemoSampleDataSeeder
{
    /// <summary>The password shared by the seeded demo accounts.</summary>
    public const string DemoPassword = "Passw0rd!";

    /// <summary>The demo users that are intentionally left outside the workspace for invitation.</summary>
    public static readonly IReadOnlyList<DemoUserSeed> Invitees =
    [
        new("alex.rivera@bsx.local", "Alex Rivera"),
        new("priya.shah@bsx.local", "Priya Shah"),
    ];

    /// <summary>Seeds the sample CRM, Sales and invite-ready user data when enabled.</summary>
    /// <param name="app">The application.</param>
    public static async Task SeedAsync(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.Environment.IsDevelopment()
            || !app.Configuration.GetValue("Demo:SeedSampleData", false))
        {
            return;
        }

        using IServiceScope scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(DemoSampleDataSeeder));
        var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var currentUser = scope.ServiceProvider.GetRequiredService<WebCurrentUser>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var catalog = scope.ServiceProvider.GetRequiredService<PermissionCatalog>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        try
        {
            User? administrator = await identity.Users
                .FirstOrDefaultAsync(candidate =>
                    EF.Property<string>(candidate, "SearchEmail") == DemoIdentitySeeder.Email)
                .ConfigureAwait(false);

            if (administrator is null)
            {
                logger.LogWarning(
                    "Demo sample data seeding skipped: sign in as {Email} is not available. Enable Demo:SeedIdentity first.",
                    DemoIdentitySeeder.Email);
                return;
            }

            currentUser.SetPrincipal(CreatePrincipal(administrator.Id.Value, catalog.Permissions.Select(item => item.Code)));

            DateTimeOffset now = timeProvider.GetUtcNow();

            foreach (DemoUserSeed invitee in Invitees)
            {
                await EnsureUserAsync(identity, sender, invitee, logger).ConfigureAwait(false);
            }

            Guid? northwindGroup = await EnsureOrganizationAsync(
                sender,
                "Northwind Group",
                "US-91-1234567",
                new AddressInput("400 Harbor Street", null, "Seattle", "WA", "98101", "US"),
                logger).ConfigureAwait(false);

            Guid? contosoHoldings = await EnsureOrganizationAsync(
                sender,
                "Contoso Holdings",
                "US-91-7654321",
                new AddressInput("1 Contoso Way", null, "Redmond", "WA", "98052", "US"),
                logger).ConfigureAwait(false);

            Guid? northwind = await EnsureCustomerAsync(
                sender,
                new DemoCustomerSeed(
                    "DEMO-NW-001",
                    "Northwind Traders",
                    "US-12-3456789",
                    northwindGroup,
                    new AddressInput("400 Harbor Street", null, "Seattle", "WA", "98101", "US"),
                    "Dana Whitfield",
                    "dana.whitfield@northwind.example",
                    "+1 206 555 0101"),
                logger).ConfigureAwait(false);

            Guid? contoso = await EnsureCustomerAsync(
                sender,
                new DemoCustomerSeed(
                    "DEMO-CT-002",
                    "Contoso Ltd",
                    "US-98-7654321",
                    contosoHoldings,
                    new AddressInput("1 Contoso Way", null, "Redmond", "WA", "98052", "US"),
                    "Mina Park",
                    "mina.park@contoso.example",
                    "+1 425 555 0144"),
                logger).ConfigureAwait(false);

            Guid? fabrikam = await EnsureCustomerAsync(
                sender,
                new DemoCustomerSeed(
                    "DEMO-FB-003",
                    "Fabrikam",
                    "US-55-1122334",
                    OrganizationId: null,
                    new AddressInput("200 Innovation Drive", null, "Chicago", "IL", "60601", "US"),
                    "Owen Brooks",
                    "owen.brooks@fabrikam.example",
                    "+1 312 555 0188"),
                logger).ConfigureAwait(false);

            Guid? northwindLead = await EnsureLeadAsync(
                sender,
                "Northwind annual services renewal",
                northwind,
                northwindGroup,
                "Renewal discussion with Dana Whitfield. Procurement is aligned on a three-year term.",
                logger).ConfigureAwait(false);

            await EnsureLeadAsync(
                sender,
                "Contoso analytics workshop",
                contoso,
                contosoHoldings,
                "Inbound request for a two-day analytics workshop for the operations team.",
                logger).ConfigureAwait(false);

            Guid? fabrikamLead = await EnsureLeadAsync(
                sender,
                "Fabrikam warehouse expansion",
                fabrikam,
                organizationId: null,
                "Discovery call confirmed the expansion timeline and budget range.",
                logger).ConfigureAwait(false);

            Guid? tailspinLead = await EnsureLeadAsync(
                sender,
                "Tailspin onboarding project",
                customerId: null,
                organizationId: null,
                "Unlinked prospect from the website. Confirm the company and primary contact next.",
                logger).ConfigureAwait(false);

            await EnsureLeadStatusAsync(sender, northwindLead, "Qualified", logger).ConfigureAwait(false);
            await EnsureLeadStatusAsync(sender, fabrikamLead, "Qualified", logger).ConfigureAwait(false);
            await EnsureLeadStatusAsync(sender, tailspinLead, "Contacted", logger).ConfigureAwait(false);

            await EnsureOpportunityAsync(
                sender,
                northwindLead,
                northwind,
                "Northwind annual services renewal",
                amount: 48_000m,
                probability: 45,
                now.AddDays(45),
                stageAdvances: 0,
                logger).ConfigureAwait(false);

            await EnsureOpportunityAsync(
                sender,
                fabrikamLead,
                fabrikam,
                "Fabrikam warehouse expansion",
                amount: 125_000m,
                probability: 60,
                now.AddDays(75),
                stageAdvances: 2,
                logger).ConfigureAwait(false);

            logger.LogInformation(
                "Demo sample data is ready for tenant {TenantId}: 3 customers, 4 leads, 2 opportunities and {InviteeCount} invite-ready users.",
                DemoIdentitySeeder.TenantId,
                Invitees.Count);
        }
        catch (Exception exception) when (exception is DbException or InvalidOperationException)
        {
            logger.LogWarning(
                exception,
                "Demo sample data seeding was skipped. Apply the Identity, CRM and Sales migrations first.");
        }
    }

    private static ClaimsPrincipal CreatePrincipal(Guid userId, IEnumerable<string> permissions)
    {
        var claims = new List<Claim>
        {
            new(WebAuthDefaults.UserIdClaim, userId.ToString()),
            new(WebAuthDefaults.EmailClaim, DemoIdentitySeeder.Email),
            new(WebAuthDefaults.NameClaim, "Demo Administrator"),
            new(WebAuthDefaults.TenantIdClaim, DemoIdentitySeeder.TenantId.ToString()),
        };

        claims.AddRange(permissions.Select(permission => new Claim(WebAuthDefaults.PermissionClaim, permission)));

        var identity = new ClaimsIdentity(
            claims,
            authenticationType: WebAuthDefaults.CookieScheme,
            nameType: WebAuthDefaults.NameClaim,
            roleType: ClaimTypes.Role);

        return new ClaimsPrincipal(identity);
    }

    private static async Task<Guid?> EnsureUserAsync(
        IdentityDbContext identity,
        ISender sender,
        DemoUserSeed seed,
        ILogger logger)
    {
        User? existing = await identity.Users
            .FirstOrDefaultAsync(candidate => EF.Property<string>(candidate, "SearchEmail") == seed.Email)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            return existing.Id.Value;
        }

        Result<RegisterUserResponse> registered = await sender.SendAsync(
            new RegisterUserCommand(seed.Email, seed.Name, DemoPassword)).ConfigureAwait(false);

        if (registered.IsFailure)
        {
            logger.LogWarning(
                "Demo user {Email} could not be created: {Error}.",
                seed.Email,
                registered.Error.Code);
            return null;
        }

        Result activated = await sender.SendAsync(
            new ActivateUserCommand(registered.Value.UserId)).ConfigureAwait(false);

        if (activated.IsFailure)
        {
            logger.LogWarning(
                "Demo user {Email} was created but could not be activated: {Error}.",
                seed.Email,
                activated.Error.Code);
        }

        return registered.Value.UserId;
    }

    private static async Task<Guid?> EnsureOrganizationAsync(
        ISender sender,
        string name,
        string taxIdentifier,
        AddressInput address,
        ILogger logger)
    {
        var existing = await sender.SendAsync(
            new ListOrganizationsQuery(1, 10, name)).ConfigureAwait(false);

        OrganizationDto? match = existing.IsSuccess
            ? existing.Value.Items.FirstOrDefault(item =>
                string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
            : null;

        if (match is not null)
        {
            return match.Id;
        }

        Result<Guid> created = await sender.SendAsync(
            new CreateOrganizationCommand(name, taxIdentifier, address)).ConfigureAwait(false);

        if (created.IsSuccess)
        {
            return created.Value;
        }

        logger.LogWarning(
            "Demo organization {Name} could not be created: {Error}.",
            name,
            created.Error.Code);

        return null;
    }

    private static async Task<Guid?> EnsureCustomerAsync(
        ISender sender,
        DemoCustomerSeed seed,
        ILogger logger)
    {
        var existing = await sender.SendAsync(
            new ListCustomersQuery(1, 10, seed.Code)).ConfigureAwait(false);

        CustomerSummaryDto? match = existing.IsSuccess
            ? existing.Value.Items.FirstOrDefault(item =>
                string.Equals(item.Code, seed.Code, StringComparison.OrdinalIgnoreCase))
            : null;

        if (match is not null)
        {
            return match.Id;
        }

        Result<Guid> created = await sender.SendAsync(
            new CreateCustomerCommand(seed.Code, seed.Name, seed.TaxIdentifier)).ConfigureAwait(false);

        if (created.IsFailure)
        {
            logger.LogWarning(
                "Demo customer {Code} could not be created: {Error}.",
                seed.Code,
                created.Error.Code);
            return null;
        }

        Guid customerId = created.Value;

        if (seed.OrganizationId is { } organizationId)
        {
            await sender.SendAsync(new AssignOrganizationCommand(customerId, organizationId))
                .ConfigureAwait(false);
        }

        await sender.SendAsync(new AddAddressCommand(customerId, seed.Address, CrmAddressType.Billing))
            .ConfigureAwait(false);

        await sender.SendAsync(
            new AddContactCommand(
                customerId,
                seed.ContactName,
                seed.ContactEmail,
                seed.ContactPhone,
                IsPrimary: true)).ConfigureAwait(false);

        return customerId;
    }

    private static async Task<Guid?> EnsureLeadAsync(
        ISender sender,
        string title,
        Guid? customerId,
        Guid? organizationId,
        string notes,
        ILogger logger)
    {
        var existing = await sender.SendAsync(
            new ListLeadsQuery(1, 10, title)).ConfigureAwait(false);

        LeadSummaryDto? match = existing.IsSuccess
            ? existing.Value.Items.FirstOrDefault(item =>
                string.Equals(item.Title, title, StringComparison.OrdinalIgnoreCase))
            : null;

        if (match is not null)
        {
            return match.Id;
        }

        Result<Guid> created = await sender.SendAsync(
            new CreateLeadCommand(title, customerId, organizationId, notes)).ConfigureAwait(false);

        if (created.IsSuccess)
        {
            return created.Value;
        }

        logger.LogWarning(
            "Demo lead {Title} could not be created: {Error}.",
            title,
            created.Error.Code);

        return null;
    }

    private static async Task EnsureLeadStatusAsync(
        ISender sender,
        Guid? leadId,
        string targetStatus,
        ILogger logger)
    {
        if (leadId is null)
        {
            return;
        }

        var leadResult = await sender.SendAsync(new GetLeadByIdQuery(leadId.Value)).ConfigureAwait(false);

        if (leadResult.IsFailure)
        {
            return;
        }

        LeadDto lead = leadResult.Value;

        if (string.Equals(lead.Status, targetStatus, StringComparison.Ordinal)
            || string.Equals(lead.Status, "Disqualified", StringComparison.Ordinal))
        {
            return;
        }

        string status = lead.Status;

        if (string.Equals(status, "New", StringComparison.Ordinal))
        {
            Result contacted = await sender.SendAsync(new MarkLeadContactedCommand(lead.Id)).ConfigureAwait(false);
            if (contacted.IsFailure)
            {
                logger.LogWarning(
                    "Demo lead {Title} could not be marked contacted: {Error}.",
                    lead.Title,
                    contacted.Error.Code);
                return;
            }

            status = "Contacted";
        }

        if (string.Equals(targetStatus, "Qualified", StringComparison.Ordinal)
            && string.Equals(status, "Contacted", StringComparison.Ordinal))
        {
            Result qualified = await sender.SendAsync(new QualifyLeadCommand(lead.Id)).ConfigureAwait(false);
            if (qualified.IsFailure)
            {
                logger.LogWarning(
                    "Demo lead {Title} could not be qualified: {Error}.",
                    lead.Title,
                    qualified.Error.Code);
            }
        }
    }

    private static async Task<Guid?> EnsureOpportunityAsync(
        ISender sender,
        Guid? leadId,
        Guid? customerId,
        string title,
        decimal amount,
        int probability,
        DateTimeOffset expectedClose,
        int stageAdvances,
        ILogger logger)
    {
        if (leadId is null || customerId is null)
        {
            return null;
        }

        var existing = await sender.SendAsync(
            new ListOpportunitiesQuery(1, 10, title)).ConfigureAwait(false);

        OpportunitySummaryDto? match = existing.IsSuccess
            ? existing.Value.Items.FirstOrDefault(item =>
                string.Equals(item.Title, title, StringComparison.OrdinalIgnoreCase))
            : null;

        if (match is not null)
        {
            return match.Id;
        }

        DateOnly closeDate = DateOnly.FromDateTime(expectedClose.UtcDateTime);

        Result<Guid> created = await sender.SendAsync(
            new CreateOpportunityFromLeadCommand(
                leadId.Value,
                customerId.Value,
                title,
                amount,
                "USD",
                probability,
                closeDate)).ConfigureAwait(false);

        if (created.IsFailure)
        {
            logger.LogWarning(
                "Demo opportunity {Title} could not be created: {Error}.",
                title,
                created.Error.Code);
            return null;
        }

        for (int index = 0; index < stageAdvances; index++)
        {
            await sender.SendAsync(new AdvanceOpportunityStageCommand(created.Value)).ConfigureAwait(false);
        }

        return created.Value;
    }

    /// <summary>A seeded user that is ready to be invited to the workspace.</summary>
    /// <param name="Email">The account email.</param>
    /// <param name="Name">The display name.</param>
    public sealed record DemoUserSeed(string Email, string Name);

    private sealed record DemoCustomerSeed(
        string Code,
        string Name,
        string TaxIdentifier,
        Guid? OrganizationId,
        AddressInput Address,
        string ContactName,
        string ContactEmail,
        string ContactPhone);
}
