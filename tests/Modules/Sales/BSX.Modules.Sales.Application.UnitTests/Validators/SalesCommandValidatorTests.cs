using BSX.Modules.Sales.Application.Leads.CreateLead;
using BSX.Modules.Sales.Application.Leads.DisqualifyLead;
using BSX.Modules.Sales.Application.Leads.MarkLeadContacted;
using BSX.Modules.Sales.Application.Leads.QualifyLead;
using BSX.Modules.Sales.Application.Leads.UpdateLead;
using BSX.Modules.Sales.Application.Opportunities.AdvanceOpportunityStage;
using BSX.Modules.Sales.Application.Opportunities.CreateOpportunityFromLead;
using BSX.Modules.Sales.Application.Opportunities.MarkOpportunityLost;
using BSX.Modules.Sales.Application.Opportunities.MarkOpportunityWon;
using BSX.Modules.Sales.Application.Opportunities.UpdateOpportunity;
using BSX.Modules.Sales.Domain.Leads;

namespace BSX.Modules.Sales.Application.UnitTests.Validators;

public sealed class SalesCommandValidatorTests
{
    [Fact]
    public async Task CreateLead_Should_BeValid_WhenValid()
    {
        var result = await new CreateLeadCommandValidator().ValidateAsync(
            new CreateLeadCommand("Deal", Guid.NewGuid(), Guid.NewGuid(), "Notes"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("Deal", true)]
    public async Task CreateLead_Should_ValidateTitle(string title, bool expected)
    {
        var result = await new CreateLeadCommandValidator().ValidateAsync(
            new CreateLeadCommand(title, null, null, null));

        result.IsValid.Should().Be(expected);
    }

    [Fact]
    public async Task CreateLead_Should_BeInvalid_WhenNotesAreTooLong()
    {
        var result = await new CreateLeadCommandValidator().ValidateAsync(
            new CreateLeadCommand("Deal", null, null, new string('n', Lead.MaxNotesLength + 1)));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CreateLead_Should_BeInvalid_WhenOptionalIdsAreEmpty()
    {
        var customer = await new CreateLeadCommandValidator().ValidateAsync(
            new CreateLeadCommand("Deal", Guid.Empty, null, null));
        var organization = await new CreateLeadCommandValidator().ValidateAsync(
            new CreateLeadCommand("Deal", null, Guid.Empty, null));

        customer.IsValid.Should().BeFalse();
        organization.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateLead_Should_BeInvalid_WhenIdIsEmpty()
    {
        var result = await new UpdateLeadCommandValidator().ValidateAsync(
            new UpdateLeadCommand(Guid.Empty, "Deal", null, null, null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateLead_Should_BeValid_WhenTerminalNotesOnlyPayloadIsWellFormed()
    {
        var result = await new UpdateLeadCommandValidator().ValidateAsync(
            new UpdateLeadCommand(Guid.NewGuid(), "Deal", null, null, "Notes"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(typeof(MarkLeadContactedCommand))]
    [InlineData(typeof(QualifyLeadCommand))]
    [InlineData(typeof(DisqualifyLeadCommand))]
    public async Task LeadLifecycleCommands_Should_BeInvalid_WhenIdIsEmpty(Type commandType)
    {
        object command = commandType.Name switch
        {
            nameof(MarkLeadContactedCommand) => new MarkLeadContactedCommand(Guid.Empty),
            nameof(QualifyLeadCommand) => new QualifyLeadCommand(Guid.Empty),
            _ => new DisqualifyLeadCommand(Guid.Empty),
        };

        FluentValidation.Results.ValidationResult result = command switch
        {
            MarkLeadContactedCommand value => await new MarkLeadContactedCommandValidator().ValidateAsync(value),
            QualifyLeadCommand value => await new QualifyLeadCommandValidator().ValidateAsync(value),
            DisqualifyLeadCommand value => await new DisqualifyLeadCommandValidator().ValidateAsync(value),
            _ => throw new InvalidOperationException(),
        };

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CreateOpportunity_Should_BeValid_WhenValid()
    {
        var result = await new CreateOpportunityFromLeadCommandValidator().ValidateAsync(
            new CreateOpportunityFromLeadCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Opportunity",
                1000m,
                "USD",
                25,
                TestData.Today.AddDays(30)));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CreateOpportunity_Should_RequireLeadAndCustomer()
    {
        var missingLead = await new CreateOpportunityFromLeadCommandValidator().ValidateAsync(
            CreateOpportunity(Guid.Empty, Guid.NewGuid()));
        var missingCustomer = await new CreateOpportunityFromLeadCommandValidator().ValidateAsync(
            CreateOpportunity(Guid.NewGuid(), Guid.Empty));

        missingLead.IsValid.Should().BeFalse();
        missingCustomer.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CreateOpportunity_Should_ValidateShapeFields()
    {
        var emptyTitle = await new CreateOpportunityFromLeadCommandValidator().ValidateAsync(
            CreateOpportunity(Guid.NewGuid(), Guid.NewGuid(), title: " "));
        var shortCurrency = await new CreateOpportunityFromLeadCommandValidator().ValidateAsync(
            CreateOpportunity(Guid.NewGuid(), Guid.NewGuid(), currency: "US"));
        var missingDate = await new CreateOpportunityFromLeadCommandValidator().ValidateAsync(
            new CreateOpportunityFromLeadCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Opportunity",
                1000m,
                "USD",
                25,
                default));

        emptyTitle.IsValid.Should().BeFalse();
        shortCurrency.IsValid.Should().BeFalse();
        missingDate.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CreateOpportunity_Should_LeaveDomainRulesToTheDomain()
    {
        // Probability range and "not in the past" are domain rules; the validator only checks shape.
        var result = await new CreateOpportunityFromLeadCommandValidator().ValidateAsync(
            CreateOpportunity(
                Guid.NewGuid(),
                Guid.NewGuid(),
                probability: 101,
                expectedCloseDate: TestData.Today.AddDays(-1)));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateOpportunity_Should_BeInvalid_WhenIdsAreEmpty()
    {
        var opportunity = await new UpdateOpportunityCommandValidator().ValidateAsync(
            UpdateOpportunity(Guid.Empty, Guid.NewGuid()));
        var customer = await new UpdateOpportunityCommandValidator().ValidateAsync(
            UpdateOpportunity(Guid.NewGuid(), Guid.Empty));

        opportunity.IsValid.Should().BeFalse();
        customer.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateOpportunity_Should_ValidateShapeFields()
    {
        var emptyTitle = await new UpdateOpportunityCommandValidator().ValidateAsync(
            UpdateOpportunity(Guid.NewGuid(), Guid.NewGuid(), title: " "));
        var emptyOrganization = await new UpdateOpportunityCommandValidator().ValidateAsync(
            UpdateOpportunity(Guid.NewGuid(), Guid.NewGuid(), organizationId: Guid.Empty));
        var emptyDate = await new UpdateOpportunityCommandValidator().ValidateAsync(
            new UpdateOpportunityCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                null,
                "Opportunity",
                1000m,
                "USD",
                25,
                default));

        emptyTitle.IsValid.Should().BeFalse();
        emptyOrganization.IsValid.Should().BeFalse();
        emptyDate.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(typeof(AdvanceOpportunityStageCommand))]
    [InlineData(typeof(MarkOpportunityWonCommand))]
    [InlineData(typeof(MarkOpportunityLostCommand))]
    public async Task OpportunityLifecycleCommands_Should_BeInvalid_WhenIdIsEmpty(Type commandType)
    {
        FluentValidation.Results.ValidationResult result = commandType.Name switch
        {
            nameof(AdvanceOpportunityStageCommand) =>
                await new AdvanceOpportunityStageCommandValidator()
                    .ValidateAsync(new AdvanceOpportunityStageCommand(Guid.Empty)),
            nameof(MarkOpportunityWonCommand) =>
                await new MarkOpportunityWonCommandValidator()
                    .ValidateAsync(new MarkOpportunityWonCommand(Guid.Empty)),
            _ => await new MarkOpportunityLostCommandValidator()
                .ValidateAsync(new MarkOpportunityLostCommand(Guid.Empty)),
        };

        result.IsValid.Should().BeFalse();
    }

    private static CreateOpportunityFromLeadCommand CreateOpportunity(
        Guid leadId,
        Guid customerId,
        string title = "Opportunity",
        string currency = "USD",
        int probability = 25,
        DateOnly? expectedCloseDate = null)
        => new(
            leadId,
            customerId,
            title,
            1000m,
            currency,
            probability,
            expectedCloseDate ?? TestData.Today.AddDays(30));

    private static UpdateOpportunityCommand UpdateOpportunity(
        Guid opportunityId,
        Guid customerId,
        Guid? organizationId = null,
        string title = "Opportunity",
        string currency = "USD",
        int probability = 25,
        DateOnly? expectedCloseDate = null)
        => new(
            opportunityId,
            customerId,
            organizationId,
            title,
            1000m,
            currency,
            probability,
            expectedCloseDate ?? TestData.Today.AddDays(30));
}
