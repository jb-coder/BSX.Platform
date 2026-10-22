using BSX.SharedKernel.Results;

namespace BSX.Modules.Sales.Application.Errors;

/// <summary>Stable error catalog for the Sales application layer.</summary>
public static class SalesApplicationErrors
{
    /// <summary>The request has no active tenant scope.</summary>
    public static Error TenantRequired => Error.Forbidden("SALES.Tenant.Required", "An active tenant is required.");

    /// <summary>The lead was not found in the tenant.</summary>
    public static Error LeadNotFound => Error.NotFound("SALES.Lead.NotFound", "The lead was not found.");

    /// <summary>The opportunity was not found in the tenant.</summary>
    public static Error OpportunityNotFound => Error.NotFound(
        "SALES.Opportunity.NotFound",
        "The opportunity was not found.");

    /// <summary>The lead identifier is invalid.</summary>
    public static Error InvalidLeadId => Error.Validation("SALES.Lead.InvalidId", "The lead identifier is invalid.");

    /// <summary>The opportunity identifier is invalid.</summary>
    public static Error InvalidOpportunityId => Error.Validation(
        "SALES.Opportunity.InvalidId",
        "The opportunity identifier is invalid.");

    /// <summary>The customer identifier is invalid.</summary>
    public static Error InvalidCustomerId => Error.Validation(
        "SALES.Customer.InvalidId",
        "The customer identifier is invalid.");

    /// <summary>The organization identifier is invalid.</summary>
    public static Error InvalidOrganizationId => Error.Validation(
        "SALES.Organization.InvalidId",
        "The organization identifier is invalid.");

    /// <summary>The qualified lead has already produced an opportunity.</summary>
    public static Error OpportunityAlreadyExistsForLead => Error.Conflict(
        "SALES.Opportunity.AlreadyExistsForLead",
        "The lead has already been converted into an opportunity.");

    /// <summary>
    /// A CRM customer or organization reference does not exist in the active tenant. Referential
    /// integrity failures are conflicts, never server errors.
    /// </summary>
    /// <param name="referenceType">The invalid reference type (Customer or Organization).</param>
    /// <param name="referenceId">The invalid reference identifier.</param>
    public static Error CrmReferenceNotFound(string referenceType, Guid referenceId) => Error.Conflict(
        $"SALES.{referenceType}.NotFound",
        $"The {referenceType} reference '{referenceId}' does not exist in the active tenant.");

    /// <summary>
    /// A concurrent modification was detected while persisting the aggregate. The caller should
    /// re-read the resource and retry (optimistic concurrency, ADR-010).
    /// </summary>
    public static Error ConcurrencyConflict => Error.Conflict(
        "SALES.ConcurrencyConflict",
        "The resource was modified by another request. Reload it and retry.");
}
