using BSX.Modules.Crm.Contracts.Permissions;
using BSX.Modules.Identity.Application.Common;
using BSX.Modules.Sales.Contracts.Permissions;
using BSX.Web.Home;

namespace BSX.Web.IntegrationTests.WebFoundation;

public sealed class HomeExperienceTests
{
    private static readonly string[] AllPermissions =
    [
        CrmPermissionCodes.CustomersRead,
        CrmPermissionCodes.CustomersCreate,
        SalesPermissionCodes.LeadsRead,
        SalesPermissionCodes.LeadsCreate,
        SalesPermissionCodes.OpportunitiesRead,
        IdentityPermissionCodes.MembershipsRead,
        IdentityPermissionCodes.MembershipsManage,
    ];

    [Fact]
    public void BuildSteps_ShouldStartAtTheCustomer_ForANewWorkspace()
    {
        var input = new GettingStartedInput(AllPermissions, 0, 0, 0, 1, null);

        IReadOnlyList<GettingStartedStep> steps = HomeExperience.BuildSteps(input);

        steps.Select(step => step.Key).Should().Equal("customer", "lead", "opportunity", "invite");
        steps[0].IsNext.Should().BeTrue();
        steps[0].ActionLabel.Should().Be("Create customer");
        steps[0].Href.Should().Be("/app/crm/customers/new");
        steps.Count(step => step.IsNext).Should().Be(1);
        HomeExperience.IsNewWorkspace(input).Should().BeTrue();
    }

    [Fact]
    public void BuildSteps_ShouldPointTheOpportunityStepAtTheQualifiedLead()
    {
        Guid leadId = Guid.NewGuid();
        var input = new GettingStartedInput(AllPermissions, 1, 1, 0, 1, leadId);

        IReadOnlyList<GettingStartedStep> steps = HomeExperience.BuildSteps(input);
        GettingStartedStep opportunity = steps.Single(step => step.Key == "opportunity");

        opportunity.IsNext.Should().BeTrue();
        opportunity.ActionLabel.Should().Be("Convert qualified lead");
        opportunity.Href.Should().Be($"/app/sales/leads/{leadId}");
    }

    [Fact]
    public void BuildSteps_ShouldShowAllStepsComplete_WhenTheWorkspaceHasRecordsAndTeammates()
    {
        var input = new GettingStartedInput(AllPermissions, 3, 4, 2, 2, null);

        IReadOnlyList<GettingStartedStep> steps = HomeExperience.BuildSteps(input);

        steps.Should().OnlyContain(step => step.IsComplete);
        steps.Should().NotContain(step => step.IsNext);
        HomeExperience.IsNewWorkspace(input).Should().BeFalse();
    }

    [Fact]
    public void BuildSteps_ShouldHideStepsTheUserCannotRead()
    {
        var input = new GettingStartedInput([], 0, 0, 0, 0, null);

        IReadOnlyList<GettingStartedStep> steps = HomeExperience.BuildSteps(input);

        steps.Should().OnlyContain(step => !step.IsAvailable);
        steps.Should().NotContain(step => step.IsNext);
    }

    [Fact]
    public void BuildSteps_ShouldSendAnEmptyOpportunityStepBackToLeads()
    {
        var input = new GettingStartedInput(AllPermissions, 1, 1, 0, 1, null);

        GettingStartedStep opportunity = HomeExperience.BuildSteps(input)
            .Single(step => step.Key == "opportunity");

        opportunity.Href.Should().Be("/app/sales/leads");
        opportunity.ActionLabel.Should().Be("Go to leads");
    }

    [Fact]
    public void BuildSteps_ShouldKeepTheInviteStepReadOnly_WhenTheUserCannotManageMemberships()
    {
        var input = new GettingStartedInput(
            [IdentityPermissionCodes.MembershipsRead],
            0,
            0,
            0,
            1,
            null);

        GettingStartedStep invite = HomeExperience.BuildSteps(input)
            .Single(step => step.Key == "invite");

        invite.Href.Should().Be("/app/admin/memberships");
        invite.ActionLabel.Should().Be("View members");
    }
}
