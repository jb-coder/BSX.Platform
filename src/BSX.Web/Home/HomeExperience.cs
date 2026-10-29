using BSX.Modules.Crm.Contracts.Permissions;
using BSX.Modules.Identity.Application.Common;
using BSX.Modules.Sales.Contracts.Permissions;

namespace BSX.Web.Home;

/// <summary>A single step in the first-run workspace path.</summary>
/// <param name="Key">A stable key for tests and rendering.</param>
/// <param name="Number">The one-based step number.</param>
/// <param name="Title">The step title.</param>
/// <param name="Description">A short explanation of the step and why it matters.</param>
/// <param name="ActionLabel">The primary action label.</param>
/// <param name="Href">The primary action destination.</param>
/// <param name="IsAvailable">Whether the user can see and act on the step.</param>
/// <param name="IsComplete">Whether the step already has data.</param>
/// <param name="IsNext">Whether this is the next incomplete step.</param>
public sealed record GettingStartedStep(
    string Key,
    int Number,
    string Title,
    string Description,
    string ActionLabel,
    string Href,
    bool IsAvailable,
    bool IsComplete,
    bool IsNext)
{
    /// <summary>A short status label for the step.</summary>
    public string StatusLabel => IsComplete ? "Done" : IsNext ? "Next" : "To do";
}

/// <summary>The workspace facts used to build the first-run path.</summary>
/// <param name="Permissions">The permissions granted to the current user.</param>
/// <param name="CustomerCount">The number of customers in the workspace.</param>
/// <param name="LeadCount">The number of leads in the workspace.</param>
/// <param name="OpportunityCount">The number of opportunities in the workspace.</param>
/// <param name="MembershipCount">The number of workspace memberships, including the current user.</param>
/// <param name="QualifiedLeadId">A qualified lead that has no opportunity yet, when known.</param>
public sealed record GettingStartedInput(
    IReadOnlyCollection<string> Permissions,
    int CustomerCount,
    int LeadCount,
    int OpportunityCount,
    int MembershipCount,
    Guid? QualifiedLeadId);

/// <summary>Builds the permission-aware first-run path shown on the workspace home page.</summary>
public static class HomeExperience
{
    /// <summary>Builds the ordered getting-started steps.</summary>
    /// <param name="input">The current workspace facts.</param>
    public static IReadOnlyList<GettingStartedStep> BuildSteps(GettingStartedInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var granted = new HashSet<string>(input.Permissions, StringComparer.Ordinal);
        bool canReadCustomers = granted.Contains(CrmPermissionCodes.CustomersRead);
        bool canReadLeads = granted.Contains(SalesPermissionCodes.LeadsRead);
        bool canReadOpportunities = granted.Contains(SalesPermissionCodes.OpportunitiesRead);
        bool canReadMemberships = granted.Contains(IdentityPermissionCodes.MembershipsRead);

        bool canCreateCustomers = granted.Contains(CrmPermissionCodes.CustomersCreate);
        bool canCreateLeads = granted.Contains(SalesPermissionCodes.LeadsCreate);
        bool canManageMemberships = granted.Contains(IdentityPermissionCodes.MembershipsManage);

        var steps = new List<GettingStartedStep>
        {
            CustomerStep(input.CustomerCount, canReadCustomers, canCreateCustomers),
            LeadStep(input.CustomerCount, input.LeadCount, canReadLeads, canCreateLeads),
            OpportunityStep(
                input.LeadCount,
                input.OpportunityCount,
                input.QualifiedLeadId,
                canReadOpportunities),
            InviteStep(input.MembershipCount, canReadMemberships, canManageMemberships),
        };

        int nextIndex = steps.FindIndex(step => step.IsAvailable && !step.IsComplete);
        if (nextIndex >= 0)
        {
            steps[nextIndex] = steps[nextIndex] with { IsNext = true };
        }

        return steps;
    }

    /// <summary>Whether the workspace has no CRM or Sales records yet.</summary>
    /// <param name="input">The current workspace facts.</param>
    public static bool IsNewWorkspace(GettingStartedInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return input.CustomerCount == 0 && input.LeadCount == 0 && input.OpportunityCount == 0;
    }

    private static GettingStartedStep CustomerStep(int customerCount, bool canRead, bool canCreate)
    {
        bool complete = customerCount > 0;
        string href = complete || !canCreate ? "/app/crm/customers" : "/app/crm/customers/new";
        string action = complete ? "View customers" : canCreate ? "Create customer" : "View customers";

        return new GettingStartedStep(
            "customer",
            1,
            "Add your first customer",
            "Customers are the companies or people you serve. Everything else in the workspace links back to them.",
            action,
            href,
            canRead,
            complete,
            IsNext: false);
    }

    private static GettingStartedStep LeadStep(
        int customerCount,
        int leadCount,
        bool canRead,
        bool canCreate)
    {
        bool complete = leadCount > 0;
        string href = complete || !canCreate ? "/app/sales/leads" : "/app/sales/leads/new";
        string action = complete ? "View leads" : canCreate ? "Create lead" : "View leads";

        return new GettingStartedStep(
            "lead",
            2,
            "Capture a lead",
            customerCount == 0
                ? "A lead is a potential opportunity. You can link a customer now or add one later."
                : "Capture a prospect, then mark them contacted and qualify the lead when they are ready.",
            action,
            href,
            canRead,
            complete,
            IsNext: false);
    }

    private static GettingStartedStep OpportunityStep(
        int leadCount,
        int opportunityCount,
        Guid? qualifiedLeadId,
        bool canRead)
    {
        bool complete = opportunityCount > 0;
        bool readyToConvert = !complete && qualifiedLeadId is not null;

        string href = complete
            ? "/app/sales/opportunities"
            : readyToConvert
                ? $"/app/sales/leads/{qualifiedLeadId}"
                : "/app/sales/leads";

        string action = complete
            ? "View opportunities"
            : readyToConvert
                ? "Convert qualified lead"
                : "Go to leads";

        string description = complete
            ? "Opportunities track the revenue, stage and expected close date of qualified work."
            : leadCount == 0
                ? "Opportunities come from qualified leads. Start by capturing a lead, then contact and qualify it."
                : readyToConvert
                    ? "A qualified lead is ready. Open it and create the opportunity to start tracking the forecast."
                    : "Open a lead, mark it contacted, then qualify it to convert it into an opportunity.";

        return new GettingStartedStep(
            "opportunity",
            3,
            "Create an opportunity",
            description,
            action,
            href,
            canRead,
            complete,
            IsNext: false);
    }

    private static GettingStartedStep InviteStep(int membershipCount, bool canRead, bool canManage)
    {
        bool complete = membershipCount > 1;
        string href = canManage ? "/app/admin/memberships/invite" : "/app/admin/memberships";
        string action = complete || !canManage ? "View members" : "Invite teammate";

        return new GettingStartedStep(
            "invite",
            4,
            "Invite your team",
            "Add a user account if needed, then invite them to this workspace. Roles control what they can access.",
            action,
            href,
            canRead,
            complete,
            IsNext: false);
    }
}
