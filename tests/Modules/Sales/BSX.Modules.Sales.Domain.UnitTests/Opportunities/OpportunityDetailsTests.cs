using BSX.Modules.Sales.Domain.Enums;
using BSX.Modules.Sales.Domain.Errors;
using BSX.Modules.Sales.Domain.Opportunities;
using BSX.Modules.Sales.Domain.ValueObjects;

namespace BSX.Modules.Sales.Domain.UnitTests.Opportunities;

public sealed class OpportunityDetailsTests
{
    [Fact]
    public void Should_UpdateDetails_WhenOpen()
    {
        Opportunity opportunity = TestData.CreateOpportunity();
        OpportunityTitle title = TestData.OpportunityTitleOf("Renewal opportunity");
        CustomerId customer = TestData.Customer();
        OrganizationId organization = TestData.Organization();

        var result = opportunity.UpdateDetails(title, customer, organization);

        result.IsSuccess.Should().BeTrue();
        opportunity.Title.Should().Be(title);
        opportunity.CustomerId.Should().Be(customer);
        opportunity.OrganizationId.Should().Be(organization);
        opportunity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Should_UpdateForecast_WhenOpen_AndRecomputeWeightedAmount()
    {
        Opportunity opportunity = TestData.CreateOpportunity(
            amount: TestData.MoneyOf(10000m),
            probability: TestData.ProbabilityOf(25));
        Money amount = TestData.MoneyOf(50000m);
        Probability probability = TestData.ProbabilityOf(60);
        ExpectedCloseDate closeDate = TestData.CloseDateOf(90);

        var result = opportunity.UpdateForecast(amount, probability, closeDate);

        result.IsSuccess.Should().BeTrue();
        opportunity.Amount.Should().Be(amount);
        opportunity.Probability.Should().Be(probability);
        opportunity.ExpectedCloseDate.Should().Be(closeDate);
        opportunity.WeightedAmount.Amount.Should().Be(30000m);
        opportunity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Should_Fail_WhenUpdatingDetailsOfATerminalOpportunity()
    {
        Opportunity opportunity = TestData.CreateWonOpportunity();
        OpportunityTitle originalTitle = opportunity.Title;

        var result = opportunity.UpdateDetails(TestData.OpportunityTitleOf("Changed"), opportunity.CustomerId, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.OpportunityTerminal);
        opportunity.Title.Should().Be(originalTitle);
    }

    [Fact]
    public void Should_Fail_WhenUpdatingForecastOfATerminalOpportunity()
    {
        Opportunity opportunity = TestData.CreateLostOpportunity();
        Money originalAmount = opportunity.Amount;
        Probability originalProbability = opportunity.Probability;
        ExpectedCloseDate originalCloseDate = opportunity.ExpectedCloseDate;

        var result = opportunity.UpdateForecast(
            TestData.MoneyOf(1m),
            TestData.ProbabilityOf(99),
            TestData.CloseDateOf(1));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.OpportunityTerminal);
        opportunity.Amount.Should().Be(originalAmount);
        opportunity.Probability.Should().Be(originalProbability);
        opportunity.ExpectedCloseDate.Should().Be(originalCloseDate);
    }

    [Fact]
    public void Should_RoundWeightedAmountToTwoDecimals()
    {
        Opportunity opportunity = TestData.CreateOpportunity(
            amount: TestData.MoneyOf(10.05m),
            probability: TestData.ProbabilityOf(33));

        opportunity.WeightedAmount.Amount.Should().Be(3.32m);
    }

    [Fact]
    public void Should_KeepStage_WhenUpdatingDetails()
    {
        Opportunity opportunity = TestData.CreateOpportunity();
        opportunity.AdvanceStage().IsSuccess.Should().BeTrue();
        opportunity.ClearDomainEvents();

        opportunity.UpdateDetails(TestData.OpportunityTitleOf("Updated"), opportunity.CustomerId, null);

        opportunity.Stage.Should().Be(OpportunityStage.Qualification);
    }
}
