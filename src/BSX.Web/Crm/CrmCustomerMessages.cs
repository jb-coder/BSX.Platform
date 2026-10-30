using BSX.SharedKernel.Results;

namespace BSX.Web.Crm;

/// <summary>Maps CRM application errors to concise, actionable page messages.</summary>
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
            "CRM.Organization.NotFound" =>
                "The organization no longer exists or is outside your workspace.",
            "CRM.Organization.InvalidId" =>
                "The organization identifier is invalid.",
            "CRM.Customer.OrganizationAlreadyAssigned" =>
                "This customer is already assigned to that organization.",
            "CRM.Customer.OrganizationNotAssigned" =>
                "This customer is not currently assigned to an organization.",
            "CRM.Customer.NotActive" =>
                "Add contacts and assign organizations only while the customer is active.",
            "CRM.Customer.ContactNotFound" =>
                "The contact no longer exists. Reload the customer to see the latest contacts.",
            "CRM.Customer.ContactEmailDuplicate" =>
                "Another contact already uses that email address. Choose a different email.",
            "CRM.Customer.ContactUnreachable" =>
                "Provide an email address or a phone number for the contact.",
            "CRM.Customer.ContactLimitReached" =>
                "This customer has reached the maximum number of contacts.",
            "CRM.Customer.AddressNotFound" =>
                "The address no longer exists. Reload the customer to see the latest addresses.",
            "CRM.Customer.AddressDuplicate" =>
                "An address with the same value and type already exists for this customer.",
            "CRM.Customer.AddressLimitReached" =>
                "This customer has reached the maximum number of addresses.",
            "CRM.Name.Invalid" =>
                "The name is invalid. Check its length and try again.",
            "CRM.TaxIdentifier.Invalid" =>
                "The tax identifier contains invalid characters or is too long.",
            "CRM.Email.Invalid" =>
                "Enter a valid email address.",
            "CRM.PhoneNumber.Invalid" =>
                "Enter a valid phone number with at least 5 digits.",
            "CRM.Address.Invalid" =>
                "The address is invalid. Address line 1, city and country are required.",
            "CRM.Tenant.Required" =>
                "Choose a workspace before managing CRM records.",
            "CRM.ConcurrencyConflict" =>
                "This record was changed by someone else. Reload it and try again.",
            "Authorization.SessionExpired" =>
                "Your session has expired. Sign in again to continue.",
            "Authorization.Unauthenticated" =>
                "Your session has expired. Sign in again to continue.",
            _ => string.IsNullOrWhiteSpace(error.Message)
                ? "The request could not be completed. Please try again."
                : error.Message,
        };
    }
}
