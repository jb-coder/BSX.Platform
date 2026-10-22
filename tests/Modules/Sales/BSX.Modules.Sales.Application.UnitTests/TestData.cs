using BSX.Modules.Sales.Application.Dtos;
using BSX.Modules.Sales.Application.UnitTests.Fakes;
using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Domain.Opportunities;
using BSX.Modules.Sales.Domain.ValueObjects;

namespace BSX.Modules.Sales.Application.UnitTests;

/// <summary>Shared builders for Sales application tests.</summary>
internal static class TestData
{
    internal static readonly Guid Tenant = Guid.NewGuid();

    internal static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    internal static readonly DateOnly Today = new(2026, 1, 1);

    internal static FixedTimeProvider Clock() => new(Now);

    internal static TenantId TenantValue(Guid? tenantId = null) => TenantId.From(tenantId ?? Tenant);

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
        Guid? id = null,
        string title = "Enterprise deal",
        string? notes = null,
        Guid? customerId = null,
        Guid? organizationId = null,
        Guid? tenantId = null)
    {
        Lead lead = Lead.Create(
            id is { } value ? LeadId.From(value) : LeadId.New(),
            TenantValue(tenantId),
            LeadTitleOf(title),
            customerId is { } customer ? Customer(customer) : null,
            organizationId is { } organization ? Organization(organization) : null,
            notes).Value;

        lead.ClearDomainEvents();
        return lead;
    }

    internal static Lead CreateContactedLead(
        Guid? id = null,
        Guid? customerId = null,
        Guid? organizationId = null,
        Guid? tenantId = null)
    {
        Lead lead = CreateLead(id, customerId: customerId, organizationId: organizationId, tenantId: tenantId);
        lead.MarkContacted().IsSuccess.Should().BeTrue();
        lead.ClearDomainEvents();
        return lead;
    }

    internal static Lead CreateQualifiedLead(
        Guid? id = null,
        Guid? customerId = null,
        Guid? organizationId = null,
        Guid? tenantId = null)
    {
        Lead lead = CreateContactedLead(id, customerId, organizationId, tenantId);
        lead.Qualify().IsSuccess.Should().BeTrue();
        lead.ClearDomainEvents();
        return lead;
    }

    internal static Opportunity CreateOpportunity(
        Lead? lead = null,
        Guid? customerId = null,
        TenantId? tenantId = null)
    {
        Guid customer = customerId ?? Guid.NewGuid();
        Lead source = lead ?? CreateQualifiedLead(customerId: customer);

        Opportunity opportunity = Opportunity.CreateFromLead(
            OpportunityId.New(),
            tenantId ?? TenantValue(),
            source,
            Customer(customer),
            OpportunityTitleOf(),
            MoneyOf(),
            ProbabilityOf(),
            CloseDateOf()).Value;

        opportunity.ClearDomainEvents();
        return opportunity;
    }

    internal static Opportunity CreateNegotiationOpportunity(
        Lead? lead = null,
        Guid? customerId = null,
        TenantId? tenantId = null)
    {
        Opportunity opportunity = CreateOpportunity(lead, customerId, tenantId);
        opportunity.AdvanceStage().IsSuccess.Should().BeTrue();
        opportunity.AdvanceStage().IsSuccess.Should().BeTrue();
        opportunity.AdvanceStage().IsSuccess.Should().BeTrue();
        opportunity.ClearDomainEvents();
        return opportunity;
    }

    internal static Opportunity CreateWonOpportunity(
        Lead? lead = null,
        Guid? customerId = null,
        TenantId? tenantId = null)
    {
        Opportunity opportunity = CreateNegotiationOpportunity(lead, customerId, tenantId);
        opportunity.MarkWon().IsSuccess.Should().BeTrue();
        opportunity.ClearDomainEvents();
        return opportunity;
    }

    internal static Opportunity CreateLostOpportunity(
        Lead? lead = null,
        Guid? customerId = null,
        TenantId? tenantId = null)
    {
        Opportunity opportunity = CreateOpportunity(lead, customerId, tenantId);
        opportunity.MarkLost().IsSuccess.Should().BeTrue();
        opportunity.ClearDomainEvents();
        return opportunity;
    }

    internal static LeadDto LeadDto(
        Guid? id = null,
        string title = "Enterprise deal",
        string status = "New",
        Guid? customerId = null,
        Guid? organizationId = null,
        string? notes = null)
        => new(
            id ?? Guid.NewGuid(),
            Tenant,
            title,
            status,
            customerId,
            organizationId,
            notes);

    internal static LeadSummaryDto LeadSummary(
        Guid? id = null,
        string title = "Enterprise deal",
        string status = "New")
        => new(id ?? Guid.NewGuid(), title, status, null, null);

    internal static OpportunityDto OpportunityDto(
        Guid? id = null,
        Guid? leadId = null,
        Guid? customerId = null,
        string stage = "Discovery")
        => new(
            id ?? Guid.NewGuid(),
            Tenant,
            leadId ?? Guid.NewGuid(),
            "Enterprise opportunity",
            customerId ?? Guid.NewGuid(),
            null,
            10000m,
            "USD",
            25,
            Today.AddDays(30),
            stage,
            2500m);

    internal static OpportunitySummaryDto OpportunitySummary(
        Guid? id = null,
        Guid? leadId = null,
        Guid? customerId = null,
        string stage = "Discovery")
        => new(
            id ?? Guid.NewGuid(),
            leadId ?? Guid.NewGuid(),
            "Enterprise opportunity",
            customerId ?? Guid.NewGuid(),
            null,
            10000m,
            "USD",
            25,
            Today.AddDays(30),
            stage,
            2500m);
}
