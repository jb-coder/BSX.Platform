using System.Data.Common;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Crm.Application.Addresses.AddAddress;
using BSX.Modules.Crm.Application.Common;
using BSX.Modules.Crm.Application.Contacts.AddContact;
using BSX.Modules.Crm.Application.Customers.AssignOrganization;
using BSX.Modules.Crm.Application.Customers.CreateCustomer;
using BSX.Modules.Crm.Application.Customers.ListCustomers;
using BSX.Modules.Crm.Application.Dtos;
using BSX.Modules.Crm.Application.Organizations.CreateOrganization;
using BSX.Modules.Crm.Application.Organizations.ListOrganizations;
using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Application.Authentication.Login;
using BSX.Modules.Identity.Application.Dtos;
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
        var tokenValidator = scope.ServiceProvider.GetRequiredService<IAccessTokenValidator>();
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

            // Authenticate through the real login pipeline instead of fabricating claims. This
            // persists a session whose identifier and permissions are then revalidated against the
            // Identity store by the regular authorization behavior on every seeded command.
            Result<AuthenticationTokensResponse> login = await sender
                .SendAsync(new LoginCommand(
                    DemoIdentitySeeder.Email,
                    DemoIdentitySeeder.Password,
                    DemoIdentitySeeder.TenantId,
                    "BSX demo sample data seeder",
                    IpAddress: null))
                .ConfigureAwait(false);

            if (login.IsFailure)
            {
                logger.LogError(
                    "Demo sample data seeding skipped: the demo administrator could not sign in: {Error}.",
                    login.Error.Code);
                return;
            }

            if (!tokenValidator.TryValidate(login.Value.AccessToken, out ValidatedAccessToken token))
            {
                logger.LogError(
                    "Demo sample data seeding skipped: the issued demo access token could not be validated.");
                return;
            }

            currentUser.SetPrincipal(WebPrincipalFactory.Create(token));

            var failures = new SeedFailureCollector();
            DateTimeOffset now = timeProvider.GetUtcNow();

            var inviteeIds = new List<Guid?>(Invitees.Count);

            foreach (DemoUserSeed invitee in Invitees)
            {
                inviteeIds.Add(await EnsureUserAsync(identity, sender, invitee, logger, failures).ConfigureAwait(false));
            }

            Guid? northwindGroup = await EnsureOrganizationAsync(
                sender,
                "Northwind Group",
                "US-91-1234567",
                new AddressInput("400 Harbor Street", null, "Seattle", "WA", "98101", "US"),
                logger,
                failures).ConfigureAwait(false);

            Guid? contosoHoldings = await EnsureOrganizationAsync(
                sender,
                "Contoso Holdings",
                "US-91-7654321",
                new AddressInput("1 Contoso Way", null, "Redmond", "WA", "98052", "US"),
                logger,
                failures).ConfigureAwait(false);

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
                logger,
                failures).ConfigureAwait(false);

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
                logger,
                failures).ConfigureAwait(false);

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
                logger,
                failures).ConfigureAwait(false);

            Guid? northwindLead = await EnsureLeadAsync(
                sender,
                "Northwind annual services renewal",
                northwind,
                northwindGroup,
                "Renewal discussion with Dana Whitfield. Procurement is aligned on a three-year term.",
                logger,
                failures).ConfigureAwait(false);

            Guid? contosoLead = await EnsureLeadAsync(
                sender,
                "Contoso analytics workshop",
                contoso,
                contosoHoldings,
                "Inbound request for a two-day analytics workshop for the operations team.",
                logger,
                failures).ConfigureAwait(false);

            Guid? fabrikamLead = await EnsureLeadAsync(
                sender,
                "Fabrikam warehouse expansion",
                fabrikam,
                organizationId: null,
                "Discovery call confirmed the expansion timeline and budget range.",
                logger,
                failures).ConfigureAwait(false);

            Guid? tailspinLead = await EnsureLeadAsync(
                sender,
                "Tailspin onboarding project",
                customerId: null,
                organizationId: null,
                "Unlinked prospect from the website. Confirm the company and primary contact next.",
                logger,
                failures).ConfigureAwait(false);

            await EnsureLeadStatusAsync(sender, northwindLead, "Qualified", logger, failures).ConfigureAwait(false);
            await EnsureLeadStatusAsync(sender, fabrikamLead, "Qualified", logger, failures).ConfigureAwait(false);
            await EnsureLeadStatusAsync(sender, tailspinLead, "Contacted", logger, failures).ConfigureAwait(false);

            Guid? northwindOpportunity = await EnsureOpportunityAsync(
                sender,
                northwindLead,
                northwind,
                "Northwind annual services renewal",
                amount: 48_000m,
                probability: 45,
                now.AddDays(45),
                stageAdvances: 0,
                logger,
                failures).ConfigureAwait(false);

            Guid? fabrikamOpportunity = await EnsureOpportunityAsync(
                sender,
                fabrikamLead,
                fabrikam,
                "Fabrikam warehouse expansion",
                amount: 125_000m,
                probability: 60,
                now.AddDays(75),
                stageAdvances: 2,
                logger,
                failures).ConfigureAwait(false);

            var problems = new List<string>(failures.Failures);

            AddMissing(problems, northwindGroup, "organization 'Northwind Group'");
            AddMissing(problems, contosoHoldings, "organization 'Contoso Holdings'");
            AddMissing(problems, northwind, "customer 'Northwind Traders'");
            AddMissing(problems, contoso, "customer 'Contoso Ltd'");
            AddMissing(problems, fabrikam, "customer 'Fabrikam'");
            AddMissing(problems, northwindLead, "lead 'Northwind annual services renewal'");
            AddMissing(problems, contosoLead, "lead 'Contoso analytics workshop'");
            AddMissing(problems, fabrikamLead, "lead 'Fabrikam warehouse expansion'");
            AddMissing(problems, tailspinLead, "lead 'Tailspin onboarding project'");
            AddMissing(problems, northwindOpportunity, "opportunity 'Northwind annual services renewal'");
            AddMissing(problems, fabrikamOpportunity, "opportunity 'Fabrikam warehouse expansion'");

            for (int index = 0; index < inviteeIds.Count; index++)
            {
                AddMissing(problems, inviteeIds[index], $"invite-ready user '{Invitees[index].Email}'");
            }

            if (problems.Count == 0)
            {
                logger.LogInformation(
                    "Demo sample data is ready for tenant {TenantId}: 3 customers, 4 leads, 2 opportunities and {InviteeCount} invite-ready users.",
                    DemoIdentitySeeder.TenantId,
                    Invitees.Count);
            }
            else
            {
                logger.LogError(
                    "Demo sample data seeding did not complete: {ProblemCount} problem(s). {Problems}",
                    problems.Count,
                    string.Join(" | ", problems));
            }
        }
        catch (Exception exception) when (exception is DbException or InvalidOperationException)
        {
            logger.LogWarning(
                exception,
                "Demo sample data seeding was skipped. Apply the Identity, CRM and Sales migrations first.");
        }
    }

    private static void AddMissing(List<string> problems, Guid? id, string description)
    {
        if (id is null)
        {
            problems.Add($"{description} was not created.");
        }
    }

    private static async Task<Guid?> EnsureUserAsync(
        IdentityDbContext identity,
        ISender sender,
        DemoUserSeed seed,
        ILogger logger,
        SeedFailureCollector failures)
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
            failures.Add($"user '{seed.Email}' registration", registered.Error);
            logger.LogWarning(
                "Demo user {Email} could not be created: {Error}.",
                seed.Email,
                registered.Error.Code);
            return null;
        }

        Guid userId = registered.Value.UserId;

        Result activated = await sender.SendAsync(
            new ActivateUserCommand(userId)).ConfigureAwait(false);

        if (activated.IsFailure)
        {
            failures.Add($"user '{seed.Email}' activation", activated.Error);
            logger.LogWarning(
                "Demo user {Email} was created but could not be activated: {Error}.",
                seed.Email,
                activated.Error.Code);
        }

        return userId;
    }

    private static async Task<Guid?> EnsureOrganizationAsync(
        ISender sender,
        string name,
        string taxIdentifier,
        AddressInput address,
        ILogger logger,
        SeedFailureCollector failures)
    {
        var existing = await sender.SendAsync(
            new ListOrganizationsQuery(1, 10, name)).ConfigureAwait(false);

        if (existing.IsFailure)
        {
            failures.Add($"organization lookup '{name}'", existing.Error);
        }

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

        failures.Add($"organization '{name}' creation", created.Error);
        logger.LogWarning(
            "Demo organization {Name} could not be created: {Error}.",
            name,
            created.Error.Code);

        return null;
    }

    private static async Task<Guid?> EnsureCustomerAsync(
        ISender sender,
        DemoCustomerSeed seed,
        ILogger logger,
        SeedFailureCollector failures)
    {
        var existing = await sender.SendAsync(
            new ListCustomersQuery(1, 10, seed.Code)).ConfigureAwait(false);

        if (existing.IsFailure)
        {
            failures.Add($"customer lookup '{seed.Code}'", existing.Error);
        }

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
            failures.Add($"customer '{seed.Code}' creation", created.Error);
            logger.LogWarning(
                "Demo customer {Code} could not be created: {Error}.",
                seed.Code,
                created.Error.Code);
            return null;
        }

        Guid customerId = created.Value;

        if (seed.OrganizationId is { } organizationId)
        {
            Result assigned = await sender.SendAsync(new AssignOrganizationCommand(customerId, organizationId))
                .ConfigureAwait(false);

            if (assigned.IsFailure)
            {
                failures.Add($"customer '{seed.Code}' organization assignment", assigned.Error);
                logger.LogWarning(
                    "Demo customer {Code} could not be assigned to its organization: {Error}.",
                    seed.Code,
                    assigned.Error.Code);
            }
        }

        Result addressed = await sender.SendAsync(
            new AddAddressCommand(customerId, seed.Address, CrmAddressType.Billing)).ConfigureAwait(false);

        if (addressed.IsFailure)
        {
            failures.Add($"customer '{seed.Code}' billing address", addressed.Error);
            logger.LogWarning(
                "Demo customer {Code} billing address could not be added: {Error}.",
                seed.Code,
                addressed.Error.Code);
        }

        Result contact = await sender.SendAsync(
            new AddContactCommand(
                customerId,
                seed.ContactName,
                seed.ContactEmail,
                seed.ContactPhone,
                IsPrimary: true)).ConfigureAwait(false);

        if (contact.IsFailure)
        {
            failures.Add($"customer '{seed.Code}' primary contact", contact.Error);
            logger.LogWarning(
                "Demo customer {Code} primary contact could not be added: {Error}.",
                seed.Code,
                contact.Error.Code);
        }

        return customerId;
    }

    private static async Task<Guid?> EnsureLeadAsync(
        ISender sender,
        string title,
        Guid? customerId,
        Guid? organizationId,
        string notes,
        ILogger logger,
        SeedFailureCollector failures)
    {
        var existing = await sender.SendAsync(
            new ListLeadsQuery(1, 10, title)).ConfigureAwait(false);

        if (existing.IsFailure)
        {
            failures.Add($"lead lookup '{title}'", existing.Error);
        }

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

        failures.Add($"lead '{title}' creation", created.Error);
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
        ILogger logger,
        SeedFailureCollector failures)
    {
        if (leadId is null)
        {
            failures.Add($"lead status '{targetStatus}'", Error.Failure(
                "Demo.Seeding.LeadMissing",
                "The lead required for the status transition was not created."));
            return;
        }

        var leadResult = await sender.SendAsync(new GetLeadByIdQuery(leadId.Value)).ConfigureAwait(false);

        if (leadResult.IsFailure)
        {
            failures.Add($"lead status lookup '{targetStatus}'", leadResult.Error);
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
                failures.Add($"lead '{lead.Title}' mark contacted", contacted.Error);
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
                failures.Add($"lead '{lead.Title}' qualification", qualified.Error);
                logger.LogWarning(
                    "Demo lead {Title} could not be qualified: {Error}.",
                    lead.Title,
                    qualified.Error.Code);
                return;
            }

            status = "Qualified";
        }

        if (!string.Equals(status, targetStatus, StringComparison.Ordinal))
        {
            failures.Add(
                $"lead '{lead.Title}' status",
                Error.Failure(
                    "Demo.Seeding.LeadStatusNotReached",
                    $"Expected status '{targetStatus}' but the lead is '{status}'."));
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
        ILogger logger,
        SeedFailureCollector failures)
    {
        if (leadId is null || customerId is null)
        {
            failures.Add($"opportunity '{title}'", Error.Failure(
                "Demo.Seeding.OpportunityReferenceMissing",
                "The lead or customer required for the opportunity was not created."));
            return null;
        }

        var existing = await sender.SendAsync(
            new ListOpportunitiesQuery(1, 10, title)).ConfigureAwait(false);

        if (existing.IsFailure)
        {
            failures.Add($"opportunity lookup '{title}'", existing.Error);
        }

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
            failures.Add($"opportunity '{title}' creation", created.Error);
            logger.LogWarning(
                "Demo opportunity {Title} could not be created: {Error}.",
                title,
                created.Error.Code);
            return null;
        }

        Guid opportunityId = created.Value;

        for (int index = 0; index < stageAdvances; index++)
        {
            Result advanced = await sender
                .SendAsync(new AdvanceOpportunityStageCommand(opportunityId))
                .ConfigureAwait(false);

            if (advanced.IsFailure)
            {
                failures.Add($"opportunity '{title}' stage advance {index + 1}", advanced.Error);
                logger.LogWarning(
                    "Demo opportunity {Title} could not advance to the next stage: {Error}.",
                    title,
                    advanced.Error.Code);
            }
        }

        return opportunityId;
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

    private sealed class SeedFailureCollector
    {
        private readonly List<string> _failures = [];

        public IReadOnlyList<string> Failures => _failures;

        public void Add(string operation, Error error)
        {
            ArgumentNullException.ThrowIfNull(error);
            _failures.Add($"{operation}: {error.Code}");
        }
    }
}
