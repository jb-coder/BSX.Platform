using BSX.SharedKernel.Results;

namespace BSX.Web.Crm;

/// <summary>Maps customer application errors to concise, actionable page messages.</summary>
internal static class CrmCustomerMessages
{
    /// <summary>Returns a user-facing message for a CRM application error.</summary>
    /// <param name="error">The application error.</param>
    public static string For(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return error.Code switch
        {
            "CRM.Customer.CodeAlreadyExists" =>
                "That customer code is already in use in this workspace. Choose a different code.",
            "CRM.Customer.NotFound" =>
                "The customer no longer exists or is outside your workspace.",
            "CRM.Customer.InvalidId" =>
                "The customer identifier is invalid.",
            "CRM.Tenant.Required" =>
                "Choose a workspace before managing customers.",
            "CRM.ConcurrencyConflict" =>
                "This customer was changed by someone else. Reload the customer and try again.",
            _ => string.IsNullOrWhiteSpace(error.Message)
                ? "The request could not be completed. Please try again."
                : error.Message,
        };
    }
}
