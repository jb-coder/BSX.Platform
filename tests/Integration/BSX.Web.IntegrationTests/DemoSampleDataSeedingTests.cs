using BSX.BuildingBlocks.Authorization;
using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Domain.Organizations;
using BSX.Modules.Crm.Infrastructure.Persistence;
using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.Sessions;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Infrastructure.Persistence;
using BSX.Modules.Sales.Domain.Enums;
using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Domain.Opportunities;
using BSX.Modules.Sales.Infrastructure.Persistence;
using BSX.Web.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BSX.Web.IntegrationTests;

/// <summary>
/// Verifies that the development-only demo sample data seeder creates the expected records through
/// the trusted authentication path, and that disabling the feature flags leaves the store untouched.
/// </summary>
public sealed class DemoSampleDataSeedingTests
{
    private const string DemoAdministratorEmail = "admin@bsx.local";

    private static readonly Guid DemoTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly string[] DemoInviteeEmails =
    [
        "alex.rivera@bsx.local",
        "priya.shah@bsx.local",
    ];

    [Fact]
    public async Task Seeding_Should_CreateExpectedRecords_WhenEnabled()
    {
        // One factory per test: the SQLite in-memory stores must not be shared between tests.
        using var factory = new IdentityApiFactory();
        using var host = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Demo:SeedIdentity"] = "true",
                    ["Demo:SeedSampleData"] = "true",
                }));

            // Exercise the production revalidator: the seeder must obtain a real persisted session
            // instead of relying on the legacy claim-only test principal.
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IAuthorizationRevalidator>();
                services.AddScoped<IAuthorizationRevalidator, WebAuthorizationRevalidator>();
            });
        });

        // Startup (and therefore the seeder) runs when the host is first used.
        using HttpClient client = host.CreateClient();
        client.BaseAddress.Should().NotBeNull();

        using IServiceScope scope = host.Services.CreateScope();
        var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var crm = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var sales = scope.ServiceProvider.GetRequiredService<SalesDbContext>();

        List<User> users = await identity.Users.IgnoreQueryFilters().ToListAsync();
        User administrator = users.Single(user => user.Email.Value == DemoAdministratorEmail);
        administrator.Status.Should().Be(UserStatus.Active);

        List<UserSession> sessions = await identity.Sessions.ToListAsync();
        sessions.Should().Contain(session =>
            session.UserId.Value == administrator.Id.Value
            && session.TenantId != null
            && session.TenantId.Value == DemoTenantId
            && session.IsActive(factory.Clock.GetUtcNow()));

        foreach (string email in DemoInviteeEmails)
        {
            User invitee = users.Single(user => user.Email.Value == email);
            invitee.Status.Should().Be(UserStatus.Active);
        }

        List<Customer> customers = await crm.Customers.IgnoreQueryFilters().ToListAsync();
        List<Organization> organizations = await crm.Organizations.IgnoreQueryFilters().ToListAsync();

        Customer northwind = customers.Single(customer => customer.Code.Value == "DEMO-NW-001");
        northwind.Name.Value.Should().Be("Northwind Traders");
        northwind.OrganizationId.Should().NotBeNull();

        Customer contoso = customers.Single(customer => customer.Code.Value == "DEMO-CT-002");
        contoso.Name.Value.Should().Be("Contoso Ltd");
        contoso.OrganizationId.Should().NotBeNull();

        Customer fabrikam = customers.Single(customer => customer.Code.Value == "DEMO-FB-003");
        fabrikam.Name.Value.Should().Be("Fabrikam");
        fabrikam.OrganizationId.Should().BeNull();

        Organization northwindGroup = organizations.Single(organization => organization.Name.Value == "Northwind Group");
        Organization contosoHoldings = organizations.Single(organization => organization.Name.Value == "Contoso Holdings");
        northwind.OrganizationId!.Value.Should().Be(northwindGroup.Id.Value);
        contoso.OrganizationId!.Value.Should().Be(contosoHoldings.Id.Value);

        List<Lead> leads = await sales.Leads.IgnoreQueryFilters().ToListAsync();
        List<Opportunity> opportunities = await sales.Opportunities.IgnoreQueryFilters().ToListAsync();

        leads.Single(lead => lead.Title.Value == "Northwind annual services renewal").Status
            .Should().Be(LeadStatus.Qualified);
        leads.Single(lead => lead.Title.Value == "Contoso analytics workshop").Status
            .Should().Be(LeadStatus.New);
        leads.Single(lead => lead.Title.Value == "Fabrikam warehouse expansion").Status
            .Should().Be(LeadStatus.Qualified);
        leads.Single(lead => lead.Title.Value == "Tailspin onboarding project").Status
            .Should().Be(LeadStatus.Contacted);

        opportunities.Single(opportunity => opportunity.Title.Value == "Northwind annual services renewal").Stage
            .Should().Be(OpportunityStage.Discovery);
        opportunities.Single(opportunity => opportunity.Title.Value == "Fabrikam warehouse expansion").Stage
            .Should().Be(OpportunityStage.Proposal);
    }

    [Fact]
    public async Task Seeding_Should_NotCreateDemoRecords_WhenDisabled()
    {
        using var factory = new IdentityApiFactory();
        using var host = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Demo:SeedIdentity"] = "false",
                    ["Demo:SeedSampleData"] = "false",
                })));

        using HttpClient client = host.CreateClient();
        client.BaseAddress.Should().NotBeNull();

        using IServiceScope scope = host.Services.CreateScope();
        var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var crm = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var sales = scope.ServiceProvider.GetRequiredService<SalesDbContext>();

        List<User> users = await identity.Users.IgnoreQueryFilters().ToListAsync();
        users.Should().NotContain(user => user.Email.Value == DemoAdministratorEmail);
        users.Should().NotContain(user => DemoInviteeEmails.Contains(user.Email.Value));

        List<Customer> customers = await crm.Customers.IgnoreQueryFilters().ToListAsync();
        List<Organization> organizations = await crm.Organizations.IgnoreQueryFilters().ToListAsync();
        customers.Should().BeEmpty();
        organizations.Should().BeEmpty();

        List<Lead> leads = await sales.Leads.IgnoreQueryFilters().ToListAsync();
        List<Opportunity> opportunities = await sales.Opportunities.IgnoreQueryFilters().ToListAsync();
        leads.Should().BeEmpty();
        opportunities.Should().BeEmpty();
    }
}
