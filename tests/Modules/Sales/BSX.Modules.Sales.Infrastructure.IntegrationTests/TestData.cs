using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Domain.Opportunities;
using BSX.Modules.Sales.Domain.ValueObjects;

namespace BSX.Modules.Sales.Infrastructure.IntegrationTests;

/// <summary>Shared builders for Sales infrastructure tests.</summary>
internal static class TestData
{
    internal static readonly Guid TenantA = Guid.NewGuid();

    internal static readonly Guid TenantB = Guid.NewGuid();

    internal static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    internal static readonly DateOnly Today = new(2026, 1, 1);

    internal static TenantId Tenant(Guid? tenantId = null) => TenantId.From(tenantId ?? TenantA);

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
        Guid? tenantId = null,
        string title = "Enterprise deal",
        string? notes = null,
        Guid? customerId = null,
        Guid? organizationId = null)
    {
        Lead lead = Lead.Create(
            id is { } value ? LeadId.From(value) : LeadId.New(),
            Tenant(tenantId),
            LeadTitleOf(title),
            customerId is { } customer ? Customer(customer) : null,
            organizationId is { } organization ? Organization(organization) : null,
            notes).Value;

        lead.ClearDomainEvents();
        return lead;
    }

    internal static Lead CreateQualifiedLead(
        Guid? id = null,
        Guid? tenantId = null,
        Guid? customerId = null,
        Guid? organizationId = null)
    {
        Lead lead = CreateLead(
            id,
            tenantId,
            customerId: customerId,
            organizationId: organizationId);

        lead.MarkContacted().IsSuccess.Should().BeTrue();
        lead.Qualify().IsSuccess.Should().BeTrue();
        lead.ClearDomainEvents();
        return lead;
    }

    internal static Opportunity CreateOpportunity(
        Lead? lead = null,
        Guid? tenantId = null,
        Guid? customerId = null,
        string title = "Enterprise opportunity",
        decimal amount = 10000m,
        string currency = "USD",
        int probability = 25,
        int closeDateDays = 30)
    {
        Guid customer = customerId ?? Guid.NewGuid();
        Lead source = lead ?? CreateQualifiedLead(tenantId: tenantId, customerId: customer);

        Opportunity opportunity = Opportunity.CreateFromLead(
            OpportunityId.New(),
            source.TenantId,
            source,
            Customer(customer),
            OpportunityTitleOf(title),
            MoneyOf(amount, currency),
            ProbabilityOf(probability),
            CloseDateOf(closeDateDays)).Value;

        opportunity.ClearDomainEvents();
        return opportunity;
    }
}
