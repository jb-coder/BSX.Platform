using BSX.Modules.Sales.Domain.DomainEvents;
using BSX.Modules.Sales.Domain.Enums;
using BSX.Modules.Sales.Domain.Errors;
using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Domain.Opportunities;
using BSX.Modules.Sales.Domain.ValueObjects;

namespace BSX.Modules.Sales.Domain.UnitTests.Opportunities;

public sealed class OpportunityCreationTests
{
    [Fact]
    public void Should_CreateFromQualifiedLead_InDiscoveryStage()
    {
        CustomerId customer = TestData.Customer();
        OrganizationId organization = TestData.Organization();
        Lead lead = TestData.CreateQualifiedLead(customerId: customer, organizationId: organization);
        OpportunityId id = OpportunityId.New();
        OpportunityTitle title = TestData.OpportunityTitleOf();
        Money amount = TestData.MoneyOf(50000m);
        Probability probability = TestData.ProbabilityOf(40);
        ExpectedCloseDate closeDate = TestData.CloseDateOf();

        var result = Opportunity.CreateFromLead(
            id,
            TestData.Tenant,
            lead,
            customer,
            title,
            amount,
            probability,
            closeDate);

        result.IsSuccess.Should().BeTrue();
        Opportunity opportunity = result.Value;
        opportunity.Id.Should().Be(id);
        opportunity.TenantId.Should().Be(TestData.Tenant);
        opportunity.LeadId.Should().Be(lead.Id);
        opportunity.Title.Should().Be(title);
        opportunity.CustomerId.Should().Be(customer);
        opportunity.OrganizationId.Should().Be(organization);
        opportunity.Amount.Should().Be(amount);
        opportunity.Probability.Should().Be(probability);
        opportunity.ExpectedCloseDate.Should().Be(closeDate);
        opportunity.Stage.Should().Be(OpportunityStage.Discovery);
        opportunity.IsTerminal.Should().BeFalse();
        opportunity.WeightedAmount.Amount.Should().Be(20000m);

        OpportunityCreated created = opportunity.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OpportunityCreated>().Which;
        created.OpportunityId.Should().Be(id);
        created.LeadId.Should().Be(lead.Id);
        created.Title.Should().Be(title);
        created.EventId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Should_Fail_WhenLeadIsNew()
    {
        Lead lead = TestData.CreateLead();

        var result = CreateFrom(lead);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.OpportunityLeadNotQualified);
    }

    [Fact]
    public void Should_Fail_WhenLeadIsContacted()
    {
        Lead lead = TestData.CreateContactedLead();

        var result = CreateFrom(lead);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.OpportunityLeadNotQualified);
    }

    [Fact]
    public void Should_Fail_WhenLeadIsDisqualified()
    {
        Lead lead = TestData.CreateLead();
        lead.Disqualify().IsSuccess.Should().BeTrue();

        var result = CreateFrom(lead);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.OpportunityLeadNotQualified);
    }

    [Fact]
    public void Should_Fail_WhenTenantDoesNotMatch()
    {
        TenantId otherTenant = TenantId.New();
        Lead lead = TestData.CreateQualifiedLead(tenantId: otherTenant);

        var result = Opportunity.CreateFromLead(
            OpportunityId.New(),
            TestData.Tenant,
            lead,
            TestData.Customer(),
            TestData.OpportunityTitleOf(),
            TestData.MoneyOf(),
            TestData.ProbabilityOf(),
            TestData.CloseDateOf());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.OpportunityTenantMismatch);
    }

    [Fact]
    public void Should_Throw_WhenLeadIsNull()
    {
        Action act = () => Opportunity.CreateFromLead(
            OpportunityId.New(),
            TestData.Tenant,
            null!,
            TestData.Customer(),
            TestData.OpportunityTitleOf(),
            TestData.MoneyOf(),
            TestData.ProbabilityOf(),
            TestData.CloseDateOf());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Should_UseTheSuppliedCustomer_AndCopyOrganizationReference()
    {
        CustomerId leadCustomer = TestData.Customer();
        CustomerId opportunityCustomer = TestData.Customer();
        OrganizationId organization = TestData.Organization();
        Lead lead = TestData.CreateQualifiedLead(customerId: leadCustomer, organizationId: organization);

        Opportunity opportunity = CreateFrom(lead, opportunityCustomer).Value;

        opportunity.CustomerId.Should().Be(opportunityCustomer);
        opportunity.OrganizationId.Should().Be(organization);
    }

    private static BSX.SharedKernel.Results.Result<Opportunity> CreateFrom(Lead lead, CustomerId? customerId = null)
        => Opportunity.CreateFromLead(
            OpportunityId.New(),
            lead.TenantId,
            lead,
            customerId ?? TestData.Customer(),
            TestData.OpportunityTitleOf(),
            TestData.MoneyOf(),
            TestData.ProbabilityOf(),
            TestData.CloseDateOf());
}
