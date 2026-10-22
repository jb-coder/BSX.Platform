using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Domain.Opportunities;
using BSX.Modules.Sales.Domain.ValueObjects;

namespace BSX.Modules.Sales.Domain.UnitTests;

/// <summary>Shared builders for Sales domain tests.</summary>
internal static class TestData
{
    internal static readonly TenantId Tenant = TenantId.New();

    internal static readonly DateOnly Today = new(2026, 1, 1);

    internal static LeadTitle LeadTitleOf(string value = "Enterprise deal") => LeadTitle.Create(value).Value;

    internal static OpportunityTitle OpportunityTitleOf(string value = "Enterprise opportunity")
        => OpportunityTitle.Create(value).Value;

    internal static Money MoneyOf(decimal amount = 10000m, string currency = "USD")
        => Money.Create(amount, currency).Value;

    internal static Probability ProbabilityOf(int percent = 25) => Probability.Create(percent).Value;

    internal static ExpectedCloseDate CloseDateOf(int days = 30)
        => ExpectedCloseDate.Create(Today.AddDays(days), Today).Value;

    internal static CustomerId Customer(Guid? id = null) => CustomerId.From(id ?? Guid.NewGuid());

    internal static OrganizationId Organization(Guid? id = null) => OrganizationId.From(id ?? Guid.NewGuid());

    internal static Lead CreateLead(
        string title = "Enterprise deal",
        string? notes = null,
        CustomerId? customerId = null,
        OrganizationId? organizationId = null,
        TenantId? tenantId = null)
    {
        Lead lead = Lead.Create(
            LeadId.New(),
            tenantId ?? Tenant,
            LeadTitleOf(title),
            customerId,
            organizationId,
            notes).Value;

        lead.ClearDomainEvents();
        return lead;
    }

    internal static Lead CreateContactedLead(
        string title = "Enterprise deal",
        CustomerId? customerId = null,
        OrganizationId? organizationId = null,
        TenantId? tenantId = null)
    {
        Lead lead = CreateLead(title, customerId: customerId, organizationId: organizationId, tenantId: tenantId);
        lead.MarkContacted().IsSuccess.Should().BeTrue();
        lead.ClearDomainEvents();
        return lead;
    }

    internal static Lead CreateQualifiedLead(
        string title = "Enterprise deal",
        CustomerId? customerId = null,
        OrganizationId? organizationId = null,
        TenantId? tenantId = null)
    {
        Lead lead = CreateContactedLead(title, customerId, organizationId, tenantId);
        lead.Qualify().IsSuccess.Should().BeTrue();
        lead.ClearDomainEvents();
        return lead;
    }

    internal static Opportunity CreateOpportunity(
        Lead? lead = null,
        OpportunityTitle? title = null,
        Money? amount = null,
        Probability? probability = null,
        ExpectedCloseDate? expectedCloseDate = null,
        TenantId? tenantId = null)
    {
        Lead source = lead ?? CreateQualifiedLead();

        Opportunity opportunity = Opportunity.CreateFromLead(
            OpportunityId.New(),
            tenantId ?? Tenant,
            source,
            title ?? OpportunityTitleOf(),
            amount ?? MoneyOf(),
            probability ?? ProbabilityOf(),
            expectedCloseDate ?? CloseDateOf()).Value;

        opportunity.ClearDomainEvents();
        return opportunity;
    }

    internal static Opportunity CreateNegotiationOpportunity()
    {
        Opportunity opportunity = CreateOpportunity();
        opportunity.AdvanceStage().IsSuccess.Should().BeTrue();
        opportunity.AdvanceStage().IsSuccess.Should().BeTrue();
        opportunity.AdvanceStage().IsSuccess.Should().BeTrue();
        opportunity.ClearDomainEvents();
        return opportunity;
    }

    internal static Opportunity CreateWonOpportunity()
    {
        Opportunity opportunity = CreateNegotiationOpportunity();
        opportunity.MarkWon().IsSuccess.Should().BeTrue();
        opportunity.ClearDomainEvents();
        return opportunity;
    }

    internal static Opportunity CreateLostOpportunity()
    {
        Opportunity opportunity = CreateOpportunity();
        opportunity.MarkLost().IsSuccess.Should().BeTrue();
        opportunity.ClearDomainEvents();
        return opportunity;
    }
}
