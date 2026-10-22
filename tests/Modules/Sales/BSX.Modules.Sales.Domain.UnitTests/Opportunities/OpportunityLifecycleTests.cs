using BSX.Modules.Sales.Domain.DomainEvents;
using BSX.Modules.Sales.Domain.Enums;
using BSX.Modules.Sales.Domain.Errors;
using BSX.Modules.Sales.Domain.Opportunities;
using BSX.Modules.Sales.Domain.ValueObjects;

namespace BSX.Modules.Sales.Domain.UnitTests.Opportunities;

public sealed class OpportunityLifecycleTests
{
    [Theory]
    [InlineData(OpportunityStage.Discovery, OpportunityStage.Qualification)]
    [InlineData(OpportunityStage.Qualification, OpportunityStage.Proposal)]
    [InlineData(OpportunityStage.Proposal, OpportunityStage.Negotiation)]
    public void Should_AdvanceToTheNextStage_AndRaiseStageChanged(
        OpportunityStage from,
        OpportunityStage to)
    {
        Opportunity opportunity = TestData.CreateOpportunity();
        AdvanceTo(opportunity, from);
        opportunity.ClearDomainEvents();

        var result = opportunity.AdvanceStage();

        result.IsSuccess.Should().BeTrue();
        opportunity.Stage.Should().Be(to);
        OpportunityStageChanged changed = opportunity.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OpportunityStageChanged>().Which;
        changed.OpportunityId.Should().Be(opportunity.Id);
        changed.From.Should().Be(from);
        changed.To.Should().Be(to);
    }

    [Fact]
    public void Should_Fail_WhenAdvancingANegotiation()
    {
        Opportunity opportunity = TestData.CreateNegotiationOpportunity();

        var result = opportunity.AdvanceStage();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.OpportunityCannotAdvance);
        opportunity.Stage.Should().Be(OpportunityStage.Negotiation);
        opportunity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Should_Fail_WhenAdvancingAWonOpportunity()
    {
        Opportunity opportunity = TestData.CreateWonOpportunity();

        var result = opportunity.AdvanceStage();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.OpportunityTerminal);
        opportunity.Stage.Should().Be(OpportunityStage.Won);
    }

    [Fact]
    public void Should_Fail_WhenAdvancingALostOpportunity()
    {
        Opportunity opportunity = TestData.CreateLostOpportunity();

        var result = opportunity.AdvanceStage();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.OpportunityTerminal);
        opportunity.Stage.Should().Be(OpportunityStage.Lost);
    }

    [Fact]
    public void Should_MarkWon_FromNegotiation_AndRaiseOpportunityWon()
    {
        Opportunity opportunity = TestData.CreateNegotiationOpportunity();

        var result = opportunity.MarkWon();

        result.IsSuccess.Should().BeTrue();
        opportunity.Stage.Should().Be(OpportunityStage.Won);
        opportunity.IsTerminal.Should().BeTrue();
        opportunity.Probability.Should().Be(Probability.Certain);
        opportunity.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<OpportunityWon>()
            .Which.OpportunityId.Should().Be(opportunity.Id);
        opportunity.DomainEvents.Should().NotContain(domainEvent => domainEvent is OpportunityStageChanged);
    }

    [Theory]
    [InlineData(OpportunityStage.Discovery)]
    [InlineData(OpportunityStage.Qualification)]
    [InlineData(OpportunityStage.Proposal)]
    public void Should_Fail_WhenMarkingWonBeforeNegotiation(OpportunityStage stage)
    {
        Opportunity opportunity = TestData.CreateOpportunity();
        AdvanceTo(opportunity, stage);
        opportunity.ClearDomainEvents();

        var result = opportunity.MarkWon();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.OpportunityInvalidStageTransition);
        opportunity.Stage.Should().Be(stage);
        opportunity.DomainEvents.Should().BeEmpty();
    }

    [Theory]
    [InlineData(OpportunityStage.Discovery)]
    [InlineData(OpportunityStage.Qualification)]
    [InlineData(OpportunityStage.Proposal)]
    [InlineData(OpportunityStage.Negotiation)]
    public void Should_MarkLost_FromEveryOpenStage(OpportunityStage stage)
    {
        Opportunity opportunity = TestData.CreateOpportunity();
        AdvanceTo(opportunity, stage);
        opportunity.ClearDomainEvents();

        var result = opportunity.MarkLost();

        result.IsSuccess.Should().BeTrue();
        opportunity.Stage.Should().Be(OpportunityStage.Lost);
        opportunity.IsTerminal.Should().BeTrue();
        opportunity.Probability.Should().Be(Probability.None);
        opportunity.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<OpportunityLost>()
            .Which.OpportunityId.Should().Be(opportunity.Id);
    }

    [Fact]
    public void Should_Fail_WhenMarkingLostAnAlreadyWonOpportunity()
    {
        Opportunity opportunity = TestData.CreateWonOpportunity();

        var result = opportunity.MarkLost();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.OpportunityTerminal);
        opportunity.Stage.Should().Be(OpportunityStage.Won);
    }

    [Fact]
    public void Should_Fail_WhenMarkingWonAnAlreadyLostOpportunity()
    {
        Opportunity opportunity = TestData.CreateLostOpportunity();

        var result = opportunity.MarkWon();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.OpportunityTerminal);
        opportunity.Stage.Should().Be(OpportunityStage.Lost);
    }

    [Fact]
    public void Should_Fail_WhenMarkingWonTwice()
    {
        Opportunity opportunity = TestData.CreateWonOpportunity();

        var result = opportunity.MarkWon();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.OpportunityTerminal);
    }

    [Fact]
    public void Should_Fail_WhenMarkingLostTwice()
    {
        Opportunity opportunity = TestData.CreateLostOpportunity();

        var result = opportunity.MarkLost();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.OpportunityTerminal);
    }

    [Fact]
    public void Should_NotChangeProbability_WhenAdvancing()
    {
        Opportunity opportunity = TestData.CreateOpportunity(probability: Probability.Create(65).Value);
        Probability original = opportunity.Probability;

        opportunity.AdvanceStage();

        opportunity.Probability.Should().Be(original);
    }

    private static void AdvanceTo(Opportunity opportunity, OpportunityStage target)
    {
        while (opportunity.Stage != target)
        {
            opportunity.AdvanceStage().IsSuccess.Should().BeTrue();
        }
    }
}
