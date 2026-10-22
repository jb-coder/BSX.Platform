using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Domain.Opportunities;
using BSX.Modules.Sales.Infrastructure.Crm;
using BSX.Modules.Sales.Infrastructure.IntegrationTests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Sales.Infrastructure.IntegrationTests;

public sealed class CrmReferenceValidationTests
{
    [Fact]
    public async Task Should_RejectAnUnknownCustomerReference()
    {
        var validator = new TestCrmReferenceValidator { AcceptAll = false };
        await using SqliteSalesDatabase database = await SqliteSalesDatabase.CreateAsync(
            crmReferenceValidator: validator);
        Lead lead = TestData.CreateLead(customerId: Guid.NewGuid());
        await database.Context.Leads.AddAsync(lead);

        Func<Task> act = () => database.Context.SaveChangesAsync();

        CrmReferenceValidationException exception = (await act.Should()
            .ThrowAsync<CrmReferenceValidationException>()).Which;
        exception.ReferenceType.Should().Be("Customer");
        exception.ReferenceId.Should().Be(lead.CustomerId!.Value);
        exception.TenantId.Should().Be(TestData.TenantA);
        validator.CustomerCalls.Should().Contain((TestData.TenantA, lead.CustomerId.Value));
    }

    [Fact]
    public async Task Should_AcceptAllowedCustomerAndOrganizationReferences()
    {
        Guid customerId = Guid.NewGuid();
        Guid organizationId = Guid.NewGuid();
        var validator = new TestCrmReferenceValidator { AcceptAll = false };
        validator.Customers.Add(customerId);
        validator.Organizations.Add(organizationId);
        await using SqliteSalesDatabase database = await SqliteSalesDatabase.CreateAsync(
            crmReferenceValidator: validator);
        Lead lead = TestData.CreateLead(customerId: customerId, organizationId: organizationId);
        await database.Context.Leads.AddAsync(lead);

        Func<Task> act = () => database.Context.SaveChangesAsync();

        await act.Should().NotThrowAsync();
        (await database.Context.Leads.CountAsync()).Should().Be(1);
        validator.OrganizationCalls.Should().Contain((TestData.TenantA, organizationId));
    }

    [Fact]
    public async Task Should_RejectAnUnknownOrganizationReference()
    {
        Guid customerId = Guid.NewGuid();
        var validator = new TestCrmReferenceValidator { AcceptAll = false };
        validator.Customers.Add(customerId);
        await using SqliteSalesDatabase database = await SqliteSalesDatabase.CreateAsync(
            crmReferenceValidator: validator);
        Lead lead = TestData.CreateLead(customerId: customerId, organizationId: Guid.NewGuid());
        await database.Context.Leads.AddAsync(lead);

        Func<Task> act = () => database.Context.SaveChangesAsync();

        CrmReferenceValidationException exception = (await act.Should()
            .ThrowAsync<CrmReferenceValidationException>()).Which;
        exception.ReferenceType.Should().Be("Organization");
    }

    [Fact]
    public async Task Should_RejectAnUnknownCustomerReference_OnAnOpportunity()
    {
        Guid customerId = Guid.NewGuid();
        var validator = new TestCrmReferenceValidator { AcceptAll = false };
        await using SqliteSalesDatabase database = await SqliteSalesDatabase.CreateAsync(
            crmReferenceValidator: validator);
        Opportunity opportunity = TestData.CreateOpportunity(customerId: customerId);
        await database.Context.Opportunities.AddAsync(opportunity);

        Func<Task> act = () => database.Context.SaveChangesAsync();

        CrmReferenceValidationException exception = (await act.Should()
            .ThrowAsync<CrmReferenceValidationException>()).Which;
        exception.ReferenceType.Should().Be("Customer");
        exception.ReferenceId.Should().Be(customerId);
    }

    [Fact]
    public async Task Should_RejectAnUnknownOrganizationReference_OnAnOpportunity()
    {
        Guid customerId = Guid.NewGuid();
        Guid organizationId = Guid.NewGuid();
        var validator = new TestCrmReferenceValidator { AcceptAll = false };
        validator.Customers.Add(customerId);
        await using SqliteSalesDatabase database = await SqliteSalesDatabase.CreateAsync(
            crmReferenceValidator: validator);
        Lead lead = TestData.CreateQualifiedLead(
            customerId: customerId,
            organizationId: organizationId);
        Opportunity opportunity = TestData.CreateOpportunity(lead: lead, customerId: customerId);
        await database.Context.Opportunities.AddAsync(opportunity);

        Func<Task> act = () => database.Context.SaveChangesAsync();

        CrmReferenceValidationException exception = (await act.Should()
            .ThrowAsync<CrmReferenceValidationException>()).Which;
        exception.ReferenceType.Should().Be("Organization");
        exception.ReferenceId.Should().Be(organizationId);
        exception.TenantId.Should().Be(TestData.TenantA);
    }
}
