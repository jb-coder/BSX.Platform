using BSX.SharedKernel.Results;

namespace BSX.Modules.Crm.Domain.Errors;

/// <summary>
/// Stable, namespaced error catalog for the CRM domain (ADR-011).
/// Codes never change once published.
/// </summary>
public static class CrmErrors
{
    /// <summary>The customer code is invalid.</summary>
    public static Error CustomerCodeInvalid => Error.Validation("CRM.CustomerCode.Invalid", "The customer code is invalid.");

    /// <summary>The party name is invalid.</summary>
    public static Error NameInvalid => Error.Validation("CRM.Name.Invalid", "The name is invalid.");

    /// <summary>The tax identifier is invalid.</summary>
    public static Error TaxIdentifierInvalid => Error.Validation("CRM.TaxIdentifier.Invalid", "The tax identifier is invalid.");

    /// <summary>The email address is invalid.</summary>
    public static Error EmailInvalid => Error.Validation("CRM.Email.Invalid", "The email address is invalid.");

    /// <summary>The phone number is invalid.</summary>
    public static Error PhoneNumberInvalid => Error.Validation("CRM.PhoneNumber.Invalid", "The phone number is invalid.");

    /// <summary>The address is invalid.</summary>
    public static Error AddressInvalid => Error.Validation("CRM.Address.Invalid", "The address is invalid.");

    /// <summary>The customer is already active.</summary>
    public static Error CustomerAlreadyActive => Error.Conflict("CRM.Customer.AlreadyActive", "The customer is already active.");

    /// <summary>The customer is already deactivated.</summary>
    public static Error CustomerAlreadyDeactivated => Error.Conflict("CRM.Customer.AlreadyDeactivated", "The customer is already deactivated.");

    /// <summary>The customer must be active for this operation.</summary>
    public static Error CustomerNotActive => Error.Conflict("CRM.Customer.NotActive", "The customer is not active.");

    /// <summary>The organization is already assigned to the customer.</summary>
    public static Error OrganizationAlreadyAssigned => Error.Conflict("CRM.Customer.OrganizationAlreadyAssigned", "The organization is already assigned.");

    /// <summary>The contact person was not found.</summary>
    public static Error ContactNotFound => Error.NotFound("CRM.Customer.ContactNotFound", "The contact person was not found.");

    /// <summary>A contact with the same email already exists.</summary>
    public static Error ContactEmailDuplicate => Error.Conflict("CRM.Customer.ContactEmailDuplicate", "A contact with this email already exists.");

    /// <summary>The customer cannot have more contact persons.</summary>
    public static Error ContactLimitReached => Error.Conflict("CRM.Customer.ContactLimitReached", "The customer cannot have more contact persons.");

    /// <summary>The customer address was not found.</summary>
    public static Error AddressNotFound => Error.NotFound("CRM.Customer.AddressNotFound", "The customer address was not found.");

    /// <summary>The address already exists for the customer.</summary>
    public static Error AddressDuplicate => Error.Conflict("CRM.Customer.AddressDuplicate", "The address already exists for this customer.");

    /// <summary>The customer cannot have more addresses.</summary>
    public static Error AddressLimitReached => Error.Conflict("CRM.Customer.AddressLimitReached", "The customer cannot have more addresses.");

    /// <summary>The contact is already assigned to the customer.</summary>
    public static Error ContactAlreadyAssigned => Error.Conflict("CRM.Contact.AlreadyAssigned", "The contact is already assigned to this customer.");

    /// <summary>The contact is not assigned to a customer.</summary>
    public static Error ContactNotAssigned => Error.Conflict("CRM.Contact.NotAssigned", "The contact is not assigned to a customer.");

    /// <summary>The contact is already active.</summary>
    public static Error ContactAlreadyActive => Error.Conflict("CRM.Contact.AlreadyActive", "The contact is already active.");

    /// <summary>The contact is already inactive.</summary>
    public static Error ContactAlreadyInactive => Error.Conflict("CRM.Contact.AlreadyInactive", "The contact is already inactive.");
}
