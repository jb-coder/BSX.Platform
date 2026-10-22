using BSX.SharedKernel.Results;

namespace BSX.Modules.Crm.Application.Errors;

/// <summary>Stable error catalog for the CRM application layer.</summary>
public static class CrmApplicationErrors
{
    /// <summary>The request has no active tenant scope.</summary>
    public static Error TenantRequired => Error.Forbidden("CRM.Tenant.Required", "An active tenant is required.");

    /// <summary>The customer was not found in the tenant.</summary>
    public static Error CustomerNotFound => Error.NotFound("CRM.Customer.NotFound", "The customer was not found.");

    /// <summary>The organization was not found in the tenant.</summary>
    public static Error OrganizationNotFound => Error.NotFound("CRM.Organization.NotFound", "The organization was not found.");

    /// <summary>A customer with the given code already exists in the tenant.</summary>
    public static Error CustomerCodeAlreadyExists => Error.Conflict(
        "CRM.Customer.CodeAlreadyExists",
        "A customer with this code already exists in the tenant.");

    /// <summary>The customer identifier is invalid.</summary>
    public static Error InvalidCustomerId => Error.Validation("CRM.Customer.InvalidId", "The customer identifier is invalid.");

    /// <summary>The organization identifier is invalid.</summary>
    public static Error InvalidOrganizationId => Error.Validation("CRM.Organization.InvalidId", "The organization identifier is invalid.");

    /// <summary>The contact identifier is invalid.</summary>
    public static Error InvalidContactId => Error.Validation("CRM.Contact.InvalidId", "The contact identifier is invalid.");

    /// <summary>The address identifier is invalid.</summary>
    public static Error InvalidAddressId => Error.Validation("CRM.Address.InvalidId", "The address identifier is invalid.");

    /// <summary>
    /// A concurrent modification was detected while persisting the aggregate. The caller should
    /// re-read the resource and retry (optimistic concurrency, ADR-010).
    /// </summary>
    public static Error ConcurrencyConflict => Error.Conflict(
        "CRM.ConcurrencyConflict",
        "The resource was modified by another request. Reload it and retry.");
}
