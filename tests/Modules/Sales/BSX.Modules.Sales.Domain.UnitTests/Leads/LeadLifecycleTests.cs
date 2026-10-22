using BSX.Modules.Sales.Domain.DomainEvents;
using BSX.Modules.Sales.Domain.Enums;
using BSX.Modules.Sales.Domain.Errors;
using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Domain.ValueObjects;

namespace BSX.Modules.Sales.Domain.UnitTests.Leads;

public sealed class LeadLifecycleTests
{
    [Fact]
    public void Should_CreateNewLead_AndRaiseLeadCreated()
    {
        LeadId id = LeadId.New();
        LeadTitle title = TestData.LeadTitleOf("  Enterprise deal  ");
        CustomerId customer = TestData.Customer();
        OrganizationId organization = TestData.Organization();

        var result = Lead.Create(id, TestData.Tenant, title, customer, organization, "  Notes  ");

        result.IsSuccess.Should().BeTrue();
        Lead lead = result.Value;
        lead.Id.Should().Be(id);
        lead.TenantId.Should().Be(TestData.Tenant);
        lead.Title.Should().Be(title);
        lead.Status.Should().Be(LeadStatus.New);
        lead.CustomerId.Should().Be(customer);
        lead.OrganizationId.Should().Be(organization);
        lead.Notes.Should().Be("Notes");
        lead.IsEditable.Should().BeTrue();

        LeadCreated created = lead.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<LeadCreated>().Which;
        created.LeadId.Should().Be(id);
        created.Title.Should().Be(title);
        created.EventId.Should().NotBe(Guid.Empty);
        created.OccurredOnUtc.Should().NotBe(default);
    }

    [Fact]
    public void Should_Fail_WhenNotesAreTooLong()
    {
        var result = Lead.Create(
            LeadId.New(),
            TestData.Tenant,
            TestData.LeadTitleOf(),
            notes: new string('n', Lead.MaxNotesLength + 1));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.LeadNotesInvalid);
    }

    [Fact]
    public void Should_MarkContacted_WhenNew()
    {
        Lead lead = TestData.CreateLead();

        var result = lead.MarkContacted();

        result.IsSuccess.Should().BeTrue();
        lead.Status.Should().Be(LeadStatus.Contacted);
        lead.IsEditable.Should().BeTrue();
        lead.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Should_Fail_WhenContactingAnAlreadyContactedLead()
    {
        Lead lead = TestData.CreateContactedLead();

        var result = lead.MarkContacted();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.LeadAlreadyContacted);
        lead.Status.Should().Be(LeadStatus.Contacted);
    }

    [Fact]
    public void Should_Fail_WhenContactingAQualifiedLead()
    {
        Lead lead = TestData.CreateQualifiedLead();

        var result = lead.MarkContacted();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.LeadCannotBeContacted);
        lead.Status.Should().Be(LeadStatus.Qualified);
    }

    [Fact]
    public void Should_Qualify_WhenContacted_AndRaiseLeadQualified()
    {
        Lead lead = TestData.CreateContactedLead();

        var result = lead.Qualify();

        result.IsSuccess.Should().BeTrue();
        lead.Status.Should().Be(LeadStatus.Qualified);
        lead.IsEditable.Should().BeFalse();
        lead.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<LeadQualified>()
            .Which.LeadId.Should().Be(lead.Id);
    }

    [Fact]
    public void Should_Fail_WhenQualifyingANewLead()
    {
        Lead lead = TestData.CreateLead();

        var result = lead.Qualify();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.LeadMustBeContacted);
        lead.Status.Should().Be(LeadStatus.New);
        lead.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Should_Fail_WhenQualifyingTwice()
    {
        Lead lead = TestData.CreateQualifiedLead();

        var result = lead.Qualify();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.LeadAlreadyQualified);
        lead.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Should_Fail_WhenQualifyingADisqualifiedLead()
    {
        Lead lead = TestData.CreateLead();
        lead.Disqualify().IsSuccess.Should().BeTrue();
        lead.ClearDomainEvents();

        var result = lead.Qualify();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.LeadCannotBeQualified);
        lead.Status.Should().Be(LeadStatus.Disqualified);
        lead.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Should_Disqualify_WhenNew_AndRaiseLeadDisqualified()
    {
        Lead lead = TestData.CreateLead();

        var result = lead.Disqualify();

        result.IsSuccess.Should().BeTrue();
        lead.Status.Should().Be(LeadStatus.Disqualified);
        lead.IsEditable.Should().BeFalse();
        lead.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<LeadDisqualified>()
            .Which.LeadId.Should().Be(lead.Id);
    }

    [Fact]
    public void Should_Disqualify_WhenContacted()
    {
        Lead lead = TestData.CreateContactedLead();

        var result = lead.Disqualify();

        result.IsSuccess.Should().BeTrue();
        lead.Status.Should().Be(LeadStatus.Disqualified);
        lead.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<LeadDisqualified>();
    }

    [Fact]
    public void Should_Fail_WhenDisqualifyingTwice()
    {
        Lead lead = TestData.CreateLead();
        lead.Disqualify().IsSuccess.Should().BeTrue();
        lead.ClearDomainEvents();

        var result = lead.Disqualify();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.LeadAlreadyDisqualified);
        lead.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Should_Fail_WhenDisqualifyingAQualifiedLead()
    {
        Lead lead = TestData.CreateQualifiedLead();

        var result = lead.Disqualify();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.LeadCannotBeDisqualified);
        lead.Status.Should().Be(LeadStatus.Qualified);
        lead.DomainEvents.Should().BeEmpty();
    }
}
