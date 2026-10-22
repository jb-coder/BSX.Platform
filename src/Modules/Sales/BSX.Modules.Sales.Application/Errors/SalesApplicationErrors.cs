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
}
