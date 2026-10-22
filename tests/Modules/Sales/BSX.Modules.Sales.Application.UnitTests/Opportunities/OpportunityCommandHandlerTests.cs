using BSX.Modules.Sales.Application.Abstractions;
using BSX.Modules.Sales.Application.Errors;
using BSX.Modules.Sales.Application.Opportunities.AdvanceOpportunityStage;
using BSX.Modules.Sales.Application.Opportunities.CreateOpportunityFromLead;
using BSX.Modules.Sales.Application.Opportunities.MarkOpportunityLost;
using BSX.Modules.Sales.Application.Opportunities.MarkOpportunityWon;
using BSX.Modules.Sales.Application.Opportunities.UpdateOpportunity;
using BSX.Modules.Sales.Application.UnitTests.Fakes;
using BSX.Modules.Sales.Domain.Enums;
using BSX.Modules.Sales.Domain.Errors;
using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Domain.Opportunities;

namespace BSX.Modules.Sales.Application.UnitTests.Opportunities;

public sealed class OpportunityCommandHandlerTests
{
    private readonly InMemoryLeadRepository _leads = new();
    private readonly InMemoryOpportunityRepository _opportunities = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task Should_CreateOpportunityFromQualifiedLead_AndPersist()
    {
        Guid leadId = Guid.NewGuid();
        Guid leadCustomer = Guid.NewGuid();
        Guid opportunityCustomer = Guid.NewGuid();
        Guid organizationId = Guid.NewGuid();
        Lead lead = TestData.CreateQualifiedLead(
            id: leadId,
            customerId: leadCustomer,
            organizationId: organizationId);
        _leads.Seed(lead);

        var result = await CreateHandler().HandleAsync(
            new CreateOpportunityFromLeadCommand(
                leadId,
                opportunityCustomer,
                "Enterprise opportunity",
                50000m,
                "USD",
                40,
                TestData.Today.AddDays(30)));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        Opportunity opportunity = _opportunities.Opportunities.Should().ContainSingle().Subject;
        opportunity.Id.Value.Should().Be(result.Value);
        opportunity.TenantId.Value.Should().Be(TestData.Tenant);
        opportunity.LeadId.Value.Should().Be(leadId);
        opportunity.CustomerId.Value.Should().Be(opportunityCustomer);
        opportunity.OrganizationId!.Value.Should().Be(organizationId);
        opportunity.Amount.Amount.Should().Be(50000m);
        opportunity.Probability.Percent.Should().Be(40);
        opportunity.ExpectedCloseDate.Value.Should().Be(TestData.Today.AddDays(30));
        opportunity.Stage.Should().Be(OpportunityStage.Discovery);
        _opportunities.LastRequestedTenant.Should().Be(TestData.TenantValue());
        _opportunities.LastRequestedLeadId.Should().Be(lead.Id);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_RequireCustomer_AndUseTheSuppliedCustomer_WhenLeadHasNone()
    {
        Guid leadId = Guid.NewGuid();
        Lead lead = TestData.CreateQualifiedLead(id: leadId);
        lead.CustomerId.Should().BeNull();
        _leads.Seed(lead);
        Guid customerId = Guid.NewGuid();

        var result = await CreateHandler().HandleAsync(CreateCommand(leadId, customerId));

        result.IsSuccess.Should().BeTrue();
        _opportunities.Opportunities.Should().ContainSingle()
            .Which.CustomerId.Value.Should().Be(customerId);
    }

    [Fact]
    public async Task Should_Fail_WhenLeadIsNotQualified()
    {
        Guid leadId = Guid.NewGuid();
        Lead lead = TestData.CreateLead(id: leadId);
        _leads.Seed(lead);

        var result = await CreateHandler().HandleAsync(
            CreateCommand(leadId, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.OpportunityLeadNotQualified);
        _opportunities.Opportunities.Should().BeEmpty();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_Fail_WhenLeadIsNotFound()
    {
        var result = await CreateHandler().HandleAsync(
            CreateCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesApplicationErrors.LeadNotFound);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_Fail_WhenOpportunityAlreadyExistsForLead()
    {
        Guid leadId = Guid.NewGuid();
        Lead lead = TestData.CreateQualifiedLead(id: leadId);
        Opportunity existing = TestData.CreateOpportunity(lead: lead);
        _leads.Seed(lead);
        _opportunities.Seed(existing);

        var result = await CreateHandler().HandleAsync(
            CreateCommand(leadId, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesApplicationErrors.OpportunityAlreadyExistsForLead);
        _opportunities.Opportunities.Should().ContainSingle();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_Fail_WhenCustomerIdIsEmpty()
    {
        var result = await CreateHandler().HandleAsync(
            CreateCommand(Guid.NewGuid(), Guid.Empty));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesApplicationErrors.InvalidCustomerId);
    }

    [Fact]
    public async Task Should_Fail_WhenLeadIdIsEmpty()
    {
        var result = await CreateHandler().HandleAsync(
            CreateCommand(Guid.Empty, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesApplicationErrors.InvalidLeadId);
    }

    [Fact]
    public async Task Should_Fail_WhenTenantIsMissing()
    {
        var handler = new CreateOpportunityFromLeadCommandHandler(
            _leads,
            _opportunities,
            _unitOfWork,
            new SalesTenantResolver(new FakeTenantContext(null)),
            TestData.Clock());

        var result = await handler.HandleAsync(CreateCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesApplicationErrors.TenantRequired);
    }

    [Theory]
    [InlineData(-1.0, "USD", 25)]
    [InlineData(100.0, "US", 25)]
    [InlineData(100.0, "USD", 101)]
    public async Task Should_Propagate_ValueObjectFailures_WhenCreating(
        double amount,
        string currency,
        int probability)
    {
        Guid leadId = Guid.NewGuid();
        _leads.Seed(TestData.CreateQualifiedLead(id: leadId));

        var result = await CreateHandler().HandleAsync(
            new CreateOpportunityFromLeadCommand(
                leadId,
                Guid.NewGuid(),
                "Opportunity",
                (decimal)amount,
                currency,
                probability,
                TestData.Today.AddDays(30)));

        result.IsFailure.Should().BeTrue();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_Fail_WhenExpectedCloseDateIsInThePast()
    {
        Guid leadId = Guid.NewGuid();
        _leads.Seed(TestData.CreateQualifiedLead(id: leadId));

        var result = await CreateHandler().HandleAsync(
            new CreateOpportunityFromLeadCommand(
                leadId,
                Guid.NewGuid(),
                "Opportunity",
                1000m,
                "USD",
                25,
                TestData.Today.AddDays(-1)));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.ExpectedCloseDateInvalid);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_UpdateOpportunityDetailsAndForecast()
    {
        Opportunity opportunity = TestData.CreateOpportunity();
        _opportunities.Seed(opportunity);
        Guid customerId = Guid.NewGuid();
        Guid organizationId = Guid.NewGuid();

        var result = await UpdateHandler().HandleAsync(
            new UpdateOpportunityCommand(
                opportunity.Id.Value,
                customerId,
                organizationId,
                "Renamed opportunity",
                75000m,
                "EUR",
                60,
                TestData.Today.AddDays(90)));

        result.IsSuccess.Should().BeTrue();
        opportunity.Title.Value.Should().Be("Renamed opportunity");
        opportunity.CustomerId.Value.Should().Be(customerId);
        opportunity.OrganizationId!.Value.Should().Be(organizationId);
        opportunity.Amount.Amount.Should().Be(75000m);
        opportunity.Amount.Currency.Should().Be("EUR");
        opportunity.Probability.Percent.Should().Be(60);
        opportunity.ExpectedCloseDate.Value.Should().Be(TestData.Today.AddDays(90));
        _opportunities.LastRequestedTenant.Should().Be(TestData.TenantValue());
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_WhenUpdatingATerminalOpportunity()
    {
        Opportunity opportunity = TestData.CreateWonOpportunity();
        _opportunities.Seed(opportunity);

        var result = await UpdateHandler().HandleAsync(
            new UpdateOpportunityCommand(
                opportunity.Id.Value,
                opportunity.CustomerId.Value,
                null,
                "Changed",
                1m,
                "USD",
                99,
                TestData.Today.AddDays(1)));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.OpportunityTerminal);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_Fail_WhenUpdatingUnknownOpportunity()
    {
        var result = await UpdateHandler().HandleAsync(
            new UpdateOpportunityCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                null,
                "Opportunity",
                1000m,
                "USD",
                25,
                TestData.Today.AddDays(30)));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesApplicationErrors.OpportunityNotFound);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_Fail_WhenUpdatingWithEmptyIds()
    {
        var emptyOpportunity = await UpdateHandler().HandleAsync(
            new UpdateOpportunityCommand(
                Guid.Empty,
                Guid.NewGuid(),
                null,
                "Opportunity",
                1000m,
                "USD",
                25,
                TestData.Today.AddDays(30)));

        emptyOpportunity.Error.Should().Be(SalesApplicationErrors.InvalidOpportunityId);

        var emptyCustomer = await UpdateHandler().HandleAsync(
            new UpdateOpportunityCommand(
                Guid.NewGuid(),
                Guid.Empty,
                null,
                "Opportunity",
                1000m,
                "USD",
                25,
                TestData.Today.AddDays(30)));

        emptyCustomer.Error.Should().Be(SalesApplicationErrors.InvalidCustomerId);
    }

    [Fact]
    public async Task Should_AdvanceOpportunityStage()
    {
        Opportunity opportunity = TestData.CreateOpportunity();
        _opportunities.Seed(opportunity);

        var result = await AdvanceHandler().HandleAsync(
            new AdvanceOpportunityStageCommand(opportunity.Id.Value));

        result.IsSuccess.Should().BeTrue();
        opportunity.Stage.Should().Be(OpportunityStage.Qualification);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_WhenAdvancingATerminalOpportunity()
    {
        Opportunity opportunity = TestData.CreateWonOpportunity();
        _opportunities.Seed(opportunity);

        var result = await AdvanceHandler().HandleAsync(
            new AdvanceOpportunityStageCommand(opportunity.Id.Value));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.OpportunityTerminal);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_MarkWon_FromNegotiation()
    {
        Opportunity opportunity = TestData.CreateNegotiationOpportunity();
        _opportunities.Seed(opportunity);

        var result = await MarkWonHandler().HandleAsync(
            new MarkOpportunityWonCommand(opportunity.Id.Value));

        result.IsSuccess.Should().BeTrue();
        opportunity.Stage.Should().Be(OpportunityStage.Won);
        opportunity.Probability.Percent.Should().Be(100);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Propagate_WhenMarkingWonBeforeNegotiation()
    {
        Opportunity opportunity = TestData.CreateOpportunity();
        _opportunities.Seed(opportunity);

        var result = await MarkWonHandler().HandleAsync(
            new MarkOpportunityWonCommand(opportunity.Id.Value));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.OpportunityInvalidStageTransition);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_MarkLost()
    {
        Opportunity opportunity = TestData.CreateOpportunity();
        _opportunities.Seed(opportunity);

        var result = await MarkLostHandler().HandleAsync(
            new MarkOpportunityLostCommand(opportunity.Id.Value));

        result.IsSuccess.Should().BeTrue();
        opportunity.Stage.Should().Be(OpportunityStage.Lost);
        opportunity.Probability.Percent.Should().Be(0);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_WhenMarkingLostATerminalOpportunity()
    {
        Opportunity opportunity = TestData.CreateLostOpportunity();
        _opportunities.Seed(opportunity);

        var result = await MarkLostHandler().HandleAsync(
            new MarkOpportunityLostCommand(opportunity.Id.Value));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.OpportunityTerminal);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_Fail_WhenLifecycleTargetIsNotFound()
    {
        Guid id = Guid.NewGuid();

        var advance = await AdvanceHandler().HandleAsync(new AdvanceOpportunityStageCommand(id));
        var won = await MarkWonHandler().HandleAsync(new MarkOpportunityWonCommand(id));
        var lost = await MarkLostHandler().HandleAsync(new MarkOpportunityLostCommand(id));

        advance.Error.Should().Be(SalesApplicationErrors.OpportunityNotFound);
        won.Error.Should().Be(SalesApplicationErrors.OpportunityNotFound);
        lost.Error.Should().Be(SalesApplicationErrors.OpportunityNotFound);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    private static CreateOpportunityFromLeadCommand CreateCommand(Guid leadId, Guid customerId)
        => new(
            leadId,
            customerId,
            "Enterprise opportunity",
            50000m,
            "USD",
            40,
            TestData.Today.AddDays(30));

    private SalesTenantResolver TenantResolver() => new(new FakeTenantContext(TestData.Tenant));

    private CreateOpportunityFromLeadCommandHandler CreateHandler()
        => new(_leads, _opportunities, _unitOfWork, TenantResolver(), TestData.Clock());

    private UpdateOpportunityCommandHandler UpdateHandler()
        => new(_opportunities, _unitOfWork, TenantResolver(), TestData.Clock());

    private AdvanceOpportunityStageCommandHandler AdvanceHandler()
        => new(_opportunities, _unitOfWork, TenantResolver());

    private MarkOpportunityWonCommandHandler MarkWonHandler()
        => new(_opportunities, _unitOfWork, TenantResolver());

    private MarkOpportunityLostCommandHandler MarkLostHandler()
        => new(_opportunities, _unitOfWork, TenantResolver());
}
