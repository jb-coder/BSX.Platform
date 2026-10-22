using BSX.Modules.Sales.Application.Abstractions;
using BSX.Modules.Sales.Application.Errors;
using BSX.Modules.Sales.Application.Leads.CreateLead;
using BSX.Modules.Sales.Application.Leads.DisqualifyLead;
using BSX.Modules.Sales.Application.Leads.MarkLeadContacted;
using BSX.Modules.Sales.Application.Leads.QualifyLead;
using BSX.Modules.Sales.Application.Leads.UpdateLead;
using BSX.Modules.Sales.Application.UnitTests.Fakes;
using BSX.Modules.Sales.Domain.Enums;
using BSX.Modules.Sales.Domain.Errors;
using BSX.Modules.Sales.Domain.Leads;

namespace BSX.Modules.Sales.Application.UnitTests.Leads;

public sealed class LeadCommandHandlerTests
{
    private readonly InMemoryLeadRepository _leads = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task Should_CreateLead_AndPersist()
    {
        Guid customerId = Guid.NewGuid();
        Guid organizationId = Guid.NewGuid();

        var result = await CreateHandler().HandleAsync(
            new CreateLeadCommand("  Enterprise deal  ", customerId, organizationId, "  Notes  "));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        Lead lead = _leads.Leads.Should().ContainSingle().Subject;
        lead.Id.Value.Should().Be(result.Value);
        lead.Title.Value.Should().Be("Enterprise deal");
        lead.Status.Should().Be(LeadStatus.New);
        lead.CustomerId!.Value.Should().Be(customerId);
        lead.OrganizationId!.Value.Should().Be(organizationId);
        lead.Notes.Should().Be("Notes");
        lead.TenantId.Value.Should().Be(TestData.Tenant);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_WhenCreatingLeadWithInvalidTitle()
    {
        var result = await CreateHandler().HandleAsync(new CreateLeadCommand(" ", null, null, null));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.LeadTitleInvalid);
        _leads.Leads.Should().BeEmpty();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_Fail_WhenCreatingLeadWithInvalidNotes()
    {
        var result = await CreateHandler().HandleAsync(
            new CreateLeadCommand("Deal", null, null, new string('n', Lead.MaxNotesLength + 1)));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.LeadNotesInvalid);
        _leads.Leads.Should().BeEmpty();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_Fail_WhenCreatingLeadWithEmptyCustomerReference()
    {
        var result = await CreateHandler().HandleAsync(
            new CreateLeadCommand("Deal", Guid.Empty, null, null));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesApplicationErrors.InvalidCustomerId);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_Fail_WhenCreatingLeadWithEmptyOrganizationReference()
    {
        var result = await CreateHandler().HandleAsync(
            new CreateLeadCommand("Deal", null, Guid.Empty, null));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesApplicationErrors.InvalidOrganizationId);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_Fail_WhenTenantIsMissing()
    {
        var handler = new CreateLeadCommandHandler(
            _leads,
            _unitOfWork,
            new SalesTenantResolver(new FakeTenantContext(null)));

        var result = await handler.HandleAsync(new CreateLeadCommand("Deal", null, null, null));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesApplicationErrors.TenantRequired);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_UpdateDetailsAndNotes_WhenLeadIsNew()
    {
        Guid leadId = Guid.NewGuid();
        Lead lead = TestData.CreateLead(id: leadId);
        _leads.Seed(lead);
        Guid customerId = Guid.NewGuid();
        Guid organizationId = Guid.NewGuid();

        var result = await UpdateHandler().HandleAsync(
            new UpdateLeadCommand(leadId, "Renamed deal", customerId, organizationId, "Updated notes"));

        result.IsSuccess.Should().BeTrue();
        lead.Title.Value.Should().Be("Renamed deal");
        lead.CustomerId!.Value.Should().Be(customerId);
        lead.OrganizationId!.Value.Should().Be(organizationId);
        lead.Notes.Should().Be("Updated notes");
        _leads.LastRequestedTenant.Should().Be(TestData.TenantValue());
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_UpdateOnlyNotes_WhenLeadIsQualified()
    {
        Guid leadId = Guid.NewGuid();
        Guid customerId = Guid.NewGuid();
        Guid organizationId = Guid.NewGuid();
        Lead lead = TestData.CreateQualifiedLead(
            id: leadId,
            customerId: customerId,
            organizationId: organizationId);
        _leads.Seed(lead);

        var result = await UpdateHandler().HandleAsync(
            new UpdateLeadCommand(
                leadId,
                lead.Title.Value,
                customerId,
                organizationId,
                "Qualified notes"));

        result.IsSuccess.Should().BeTrue();
        lead.Notes.Should().Be("Qualified notes");
        lead.Status.Should().Be(LeadStatus.Qualified);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_WhenChangingDetailsOfATerminalLead()
    {
        Guid leadId = Guid.NewGuid();
        Guid customerId = Guid.NewGuid();
        Lead lead = TestData.CreateQualifiedLead(id: leadId, customerId: customerId);
        _leads.Seed(lead);

        var result = await UpdateHandler().HandleAsync(
            new UpdateLeadCommand(leadId, "Changed title", customerId, null, "Notes"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.LeadNotEditable);
        lead.Title.Value.Should().NotBe("Changed title");
        lead.Notes.Should().BeNull();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_Fail_WhenUpdatingUnknownLead()
    {
        var result = await UpdateHandler().HandleAsync(
            new UpdateLeadCommand(Guid.NewGuid(), "Deal", null, null, null));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesApplicationErrors.LeadNotFound);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_Fail_WhenUpdatingWithEmptyLeadId()
    {
        var result = await UpdateHandler().HandleAsync(
            new UpdateLeadCommand(Guid.Empty, "Deal", null, null, null));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesApplicationErrors.InvalidLeadId);
    }

    [Fact]
    public async Task Should_MarkContacted()
    {
        Guid leadId = Guid.NewGuid();
        Lead lead = TestData.CreateLead(id: leadId);
        _leads.Seed(lead);

        var result = await MarkContactedHandler().HandleAsync(new MarkLeadContactedCommand(leadId));

        result.IsSuccess.Should().BeTrue();
        lead.Status.Should().Be(LeadStatus.Contacted);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Propagate_WhenContactingAnAlreadyContactedLead()
    {
        Guid leadId = Guid.NewGuid();
        Lead lead = TestData.CreateContactedLead(id: leadId);
        _leads.Seed(lead);

        var result = await MarkContactedHandler().HandleAsync(new MarkLeadContactedCommand(leadId));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.LeadAlreadyContacted);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_QualifyContactedLead()
    {
        Guid leadId = Guid.NewGuid();
        Lead lead = TestData.CreateContactedLead(id: leadId);
        _leads.Seed(lead);

        var result = await QualifyHandler().HandleAsync(new QualifyLeadCommand(leadId));

        result.IsSuccess.Should().BeTrue();
        lead.Status.Should().Be(LeadStatus.Qualified);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Propagate_WhenQualifyingANewLead()
    {
        Guid leadId = Guid.NewGuid();
        Lead lead = TestData.CreateLead(id: leadId);
        _leads.Seed(lead);

        var result = await QualifyHandler().HandleAsync(new QualifyLeadCommand(leadId));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.LeadMustBeContacted);
        lead.Status.Should().Be(LeadStatus.New);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_Propagate_WhenQualifyingADisqualifiedLead()
    {
        Guid leadId = Guid.NewGuid();
        Lead lead = TestData.CreateLead(id: leadId);
        lead.Disqualify().IsSuccess.Should().BeTrue();
        _leads.Seed(lead);

        var result = await QualifyHandler().HandleAsync(new QualifyLeadCommand(leadId));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.LeadCannotBeQualified);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_DisqualifyLead()
    {
        Guid leadId = Guid.NewGuid();
        Lead lead = TestData.CreateLead(id: leadId);
        _leads.Seed(lead);

        var result = await DisqualifyHandler().HandleAsync(new DisqualifyLeadCommand(leadId));

        result.IsSuccess.Should().BeTrue();
        lead.Status.Should().Be(LeadStatus.Disqualified);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Propagate_WhenDisqualifyingAQualifiedLead()
    {
        Guid leadId = Guid.NewGuid();
        Lead lead = TestData.CreateQualifiedLead(id: leadId);
        _leads.Seed(lead);

        var result = await DisqualifyHandler().HandleAsync(new DisqualifyLeadCommand(leadId));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.LeadCannotBeDisqualified);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_Fail_WhenLeadIsNotFound()
    {
        Guid leadId = Guid.NewGuid();

        var markContacted = await MarkContactedHandler().HandleAsync(new MarkLeadContactedCommand(leadId));
        var qualify = await QualifyHandler().HandleAsync(new QualifyLeadCommand(leadId));
        var disqualify = await DisqualifyHandler().HandleAsync(new DisqualifyLeadCommand(leadId));

        markContacted.Error.Should().Be(SalesApplicationErrors.LeadNotFound);
        qualify.Error.Should().Be(SalesApplicationErrors.LeadNotFound);
        disqualify.Error.Should().Be(SalesApplicationErrors.LeadNotFound);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    private SalesTenantResolver TenantResolver() => new(new FakeTenantContext(TestData.Tenant));

    private CreateLeadCommandHandler CreateHandler()
        => new(_leads, _unitOfWork, TenantResolver());

    private UpdateLeadCommandHandler UpdateHandler()
        => new(_leads, _unitOfWork, TenantResolver());

    private MarkLeadContactedCommandHandler MarkContactedHandler()
        => new(_leads, _unitOfWork, TenantResolver());

    private QualifyLeadCommandHandler QualifyHandler()
        => new(_leads, _unitOfWork, TenantResolver());

    private DisqualifyLeadCommandHandler DisqualifyHandler()
        => new(_leads, _unitOfWork, TenantResolver());
}
