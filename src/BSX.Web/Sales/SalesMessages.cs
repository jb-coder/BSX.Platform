using BSX.SharedKernel.Results;

namespace BSX.Web.Sales;

/// <summary>Maps Sales application and domain errors to concise, actionable page messages.</summary>
internal static class SalesMessages
{
    /// <summary>Returns a user-facing message for a Sales error.</summary>
    /// <param name="error">The application or domain error.</param>
    public static string For(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return error.Code switch
        {
            "SALES.Lead.NotFound" => "The lead no longer exists or is outside your workspace.",
            "SALES.Lead.InvalidId" => "The lead identifier is invalid.",
            "SALES.Lead.AlreadyContacted" => "This lead has already been marked as contacted.",
            "SALES.Lead.CannotBeContacted" => "Only a new lead can be marked as contacted.",
            "SALES.Lead.MustBeContacted" => "Contact the lead before trying to qualify it.",
            "SALES.Lead.AlreadyQualified" => "This lead has already been qualified.",
            "SALES.Lead.CannotBeQualified" => "A disqualified lead cannot be qualified.",
            "SALES.Lead.AlreadyDisqualified" => "This lead has already been disqualified.",
            "SALES.Lead.CannotBeDisqualified" => "A qualified lead cannot be disqualified.",
            "SALES.Lead.NotEditable" => "Details can be edited only while the lead is new or contacted. Only notes can change now.",
            "SALES.Lead.NotesInvalid" => "Notes must be 2,000 characters or fewer.",
            "SALES.LeadTitle.Invalid" => "Enter a valid lead title of 200 characters or fewer.",
            "SALES.Opportunity.NotFound" => "The opportunity no longer exists or is outside your workspace.",
            "SALES.Opportunity.InvalidId" => "The opportunity identifier is invalid.",
            "SALES.Opportunity.AlreadyExistsForLead" => "This qualified lead already has an opportunity.",
            "SALES.Opportunity.LeadNotQualified" => "An opportunity can only be created from a qualified lead.",
            "SALES.Opportunity.Terminal" => "This opportunity is won or lost and can no longer be changed.",
            "SALES.Opportunity.InvalidStageTransition" => "That lifecycle action is not valid for the current stage.",
            "SALES.Opportunity.CannotAdvance" => "A negotiation must be marked won or lost; it cannot move forward again.",
            "SALES.OpportunityTitle.Invalid" => "Enter a valid opportunity title of 200 characters or fewer.",
            "SALES.Money.AmountInvalid" => "Enter a non-negative amount with at most two decimal places.",
            "SALES.Money.CurrencyInvalid" => "Enter a three-letter ISO currency code, such as USD or EUR.",
            "SALES.Probability.Invalid" => "Probability must be between 0 and 100.",
            "SALES.ExpectedCloseDate.Invalid" => "The expected close date must be today or later, within the 10-year horizon.",
            "SALES.Customer.InvalidId" => "The selected customer is invalid.",
            "SALES.Customer.NotFound" => "The selected customer no longer exists in this workspace.",
            "SALES.Organization.InvalidId" => "The selected organization is invalid.",
            "SALES.Organization.NotFound" => "The selected organization no longer exists in this workspace.",
            "SALES.Tenant.Required" => "Choose a workspace before managing sales records.",
            "SALES.ConcurrencyConflict" => "This record was changed by someone else. Reload it and try again.",
            "Authorization.SessionExpired" => "Your session has expired. Sign in again to continue.",
            "Authorization.Unauthenticated" => "Your session has expired. Sign in again to continue.",
            _ => string.IsNullOrWhiteSpace(error.Message)
                ? "The request could not be completed. Please try again."
                : error.Message,
        };
    }
}
