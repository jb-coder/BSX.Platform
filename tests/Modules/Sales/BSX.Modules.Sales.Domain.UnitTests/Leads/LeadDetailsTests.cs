using BSX.Modules.Sales.Domain.Enums;
using BSX.Modules.Sales.Domain.Errors;
using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Domain.ValueObjects;

namespace BSX.Modules.Sales.Domain.UnitTests.Leads;

public sealed class LeadDetailsTests
{
    [Fact]
    public void Should_UpdateDetails_WhenNew()
    {
        Lead lead = TestData.CreateLead();
        LeadTitle title = TestData.LeadTitleOf("Updated deal");
        CustomerId customer = TestData.Customer();
        OrganizationId organization = TestData.Organization();

        var result = lead.UpdateDetails(title, customer, organization);

        result.IsSuccess.Should().BeTrue();
        lead.Title.Should().Be(title);
        lead.CustomerId.Should().Be(customer);
        lead.OrganizationId.Should().Be(organization);
        lead.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Should_UpdateDetails_WhenContacted()
    {
        Lead lead = TestData.CreateContactedLead();
        CustomerId customer = TestData.Customer();

        var result = lead.UpdateDetails(TestData.LeadTitleOf("Contacted deal"), customer, null);

        result.IsSuccess.Should().BeTrue();
        lead.Title.Value.Should().Be("Contacted deal");
        lead.CustomerId.Should().Be(customer);
        lead.OrganizationId.Should().BeNull();
        lead.Status.Should().Be(LeadStatus.Contacted);
    }

    [Fact]
    public void Should_Fail_WhenUpdatingAQualifiedLead()
    {
        Lead lead = TestData.CreateQualifiedLead();
        LeadTitle originalTitle = lead.Title;

        var result = lead.UpdateDetails(TestData.LeadTitleOf("Changed"), null, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.LeadNotEditable);
        lead.Title.Should().Be(originalTitle);
    }

    [Fact]
    public void Should_Fail_WhenUpdatingADisqualifiedLead()
    {
        Lead lead = TestData.CreateLead();
        lead.Disqualify().IsSuccess.Should().BeTrue();
        LeadTitle originalTitle = lead.Title;

        var result = lead.UpdateDetails(TestData.LeadTitleOf("Changed"), null, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.LeadNotEditable);
        lead.Title.Should().Be(originalTitle);
    }

    [Fact]
    public void Should_AllowNotes_WhenNew()
    {
        Lead lead = TestData.CreateLead();

        var result = lead.ChangeNotes("  New notes  ");

        result.IsSuccess.Should().BeTrue();
        lead.Notes.Should().Be("New notes");
        lead.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Should_AllowNotes_WhenContacted()
    {
        Lead lead = TestData.CreateContactedLead();

        lead.ChangeNotes("Contact notes").IsSuccess.Should().BeTrue();
        lead.Notes.Should().Be("Contact notes");
    }

    [Fact]
    public void Should_AllowNotes_WhenQualified()
    {
        Lead lead = TestData.CreateQualifiedLead();

        lead.ChangeNotes("Qualified notes").IsSuccess.Should().BeTrue();
        lead.Notes.Should().Be("Qualified notes");
        lead.Status.Should().Be(LeadStatus.Qualified);
    }

    [Fact]
    public void Should_AllowNotes_WhenDisqualified()
    {
        Lead lead = TestData.CreateLead();
        lead.Disqualify().IsSuccess.Should().BeTrue();

        lead.ChangeNotes("Disqualified notes").IsSuccess.Should().BeTrue();
        lead.Notes.Should().Be("Disqualified notes");
        lead.Status.Should().Be(LeadStatus.Disqualified);
    }

    [Fact]
    public void Should_ClearNotes_WhenNull()
    {
        Lead lead = TestData.CreateLead(notes: "Existing notes");

        var result = lead.ChangeNotes(null);

        result.IsSuccess.Should().BeTrue();
        lead.Notes.Should().BeNull();
    }

    [Fact]
    public void Should_Fail_WhenNotesAreTooLong_AndPreserveExistingNotes()
    {
        Lead lead = TestData.CreateLead(notes: "Existing notes");

        var result = lead.ChangeNotes(new string('n', Lead.MaxNotesLength + 1));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.LeadNotesInvalid);
        lead.Notes.Should().Be("Existing notes");
    }
}
