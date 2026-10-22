using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Domain.Opportunities;
using BSX.Modules.Sales.Infrastructure.IntegrationTests.TestInfrastructure;
using BSX.Modules.Sales.Infrastructure.Repositories;

namespace BSX.Modules.Sales.Infrastructure.IntegrationTests;

public sealed class RepositoryTests
{
    [Fact]
    public async Task Should_RoundTripLead_WithReferencesAndNotes()
    {
        await using SqliteSalesDatabase database = await SqliteSalesDatabase.CreateAsync();
        Guid customerId = Guid.NewGuid();
        Guid organizationId = Guid.NewGuid();
        Lead lead = TestData.CreateLead(
            customerId: customerId,
            organizationId: organizationId,
            notes: "Warm lead");
        await database.Context.Leads.AddAsync(lead);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var repository = new LeadRepository(database.Context);
        Lead? loaded = await repository.GetByIdAsync(TestData.Tenant(), lead.Id);

        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(lead.Id);
        loaded.Title.Value.Should().Be("Enterprise deal");
        loaded.Status.Should().Be(BSX.Modules.Sales.Domain.Enums.LeadStatus.New);
        loaded.CustomerId!.Value.Should().Be(customerId);
        loaded.OrganizationId!.Value.Should().Be(organizationId);
        loaded.Notes.Should().Be("Warm lead");
    }

    [Fact]
    public async Task Should_RoundTripOpportunity_WithMoneyAndForecast()
    {
        await using SqliteSalesDatabase database = await SqliteSalesDatabase.CreateAsync();
        Guid customerId = Guid.NewGuid();
        Guid organizationId = Guid.NewGuid();
        Lead lead = TestData.CreateQualifiedLead(
            customerId: customerId,
            organizationId: organizationId);
        await database.Context.Leads.AddAsync(lead);
        await database.Context.SaveChangesAsync();

        Opportunity opportunity = TestData.CreateOpportunity(
            lead,
            customerId: customerId,
            amount: 50000m,
            currency: "EUR",
            probability: 40);
        await database.Context.Opportunities.AddAsync(opportunity);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var repository = new OpportunityRepository(database.Context);
        Opportunity? loaded = await repository.GetByIdAsync(TestData.Tenant(), opportunity.Id);

        loaded.Should().NotBeNull();
        loaded!.LeadId.Should().Be(lead.Id);
        loaded.CustomerId.Value.Should().Be(customerId);
        loaded.OrganizationId!.Value.Should().Be(organizationId);
        loaded.Amount.Amount.Should().Be(50000m);
        loaded.Amount.Currency.Should().Be("EUR");
        loaded.Probability.Percent.Should().Be(40);
        loaded.ExpectedCloseDate.Value.Should().Be(TestData.Today.AddDays(30));
        loaded.Stage.Should().Be(BSX.Modules.Sales.Domain.Enums.OpportunityStage.Discovery);
        loaded.WeightedAmount.Amount.Should().Be(20000m);
    }

    [Fact]
    public async Task Should_PersistUpdatedOpportunityDetailsAndForecast()
    {
        await using SqliteSalesDatabase database = await SqliteSalesDatabase.CreateAsync();
        Guid customerId = Guid.NewGuid();
        Lead lead = TestData.CreateQualifiedLead(customerId: customerId);
        await database.Context.Leads.AddAsync(lead);
        Opportunity opportunity = TestData.CreateOpportunity(lead, customerId: customerId);
        await database.Context.Opportunities.AddAsync(opportunity);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var repository = new OpportunityRepository(database.Context);
        Opportunity loaded = (await repository.GetByIdAsync(TestData.Tenant(), opportunity.Id))!;

        loaded.UpdateDetails(
            TestData.OpportunityTitleOf("Updated opportunity"),
            TestData.Customer(customerId),
            organizationId: null).IsSuccess.Should().BeTrue();
        loaded.UpdateForecast(
            TestData.MoneyOf(2000m, "EUR"),
            TestData.ProbabilityOf(75),
            TestData.CloseDateOf(45)).IsSuccess.Should().BeTrue();

        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        Opportunity reloaded = (await repository.GetByIdAsync(TestData.Tenant(), opportunity.Id))!;
        reloaded.Title.Value.Should().Be("Updated opportunity");
        reloaded.Amount.Amount.Should().Be(2000m);
        reloaded.Amount.Currency.Should().Be("EUR");
        reloaded.Probability.Percent.Should().Be(75);
        reloaded.ExpectedCloseDate.Value.Should().Be(TestData.Today.AddDays(45));
    }

    [Fact]
    public async Task Should_NotReturnAggregatesFromAnotherTenant()
    {
        await using SqliteSalesDatabase database = await SqliteSalesDatabase.CreateAsync();
        Lead lead = TestData.CreateLead(tenantId: TestData.TenantA);
        await database.Context.Leads.AddAsync(lead);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        await using BSX.Modules.Sales.Infrastructure.Persistence.SalesDbContext tenantB =
            database.CreateContext(TestData.TenantB);
        var leadRepository = new LeadRepository(tenantB);

        (await leadRepository.GetByIdAsync(TestData.Tenant(TestData.TenantB), lead.Id)).Should().BeNull();
    }

    [Fact]
    public async Task Should_ScopeOpportunityExistenceByTenantAndLead()
    {
        await using SqliteSalesDatabase database = await SqliteSalesDatabase.CreateAsync();
        Lead lead = TestData.CreateQualifiedLead();
        Opportunity opportunity = TestData.CreateOpportunity(lead: lead);
        await database.Context.Leads.AddAsync(lead);
        await database.Context.Opportunities.AddAsync(opportunity);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var repository = new OpportunityRepository(database.Context);

        (await repository.ExistsByLeadIdAsync(TestData.Tenant(TestData.TenantA), lead.Id)).Should().BeTrue();
        (await repository.ExistsByLeadIdAsync(TestData.Tenant(TestData.TenantB), lead.Id)).Should().BeFalse();
    }
}
