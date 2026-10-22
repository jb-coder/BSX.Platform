using BSX.SharedKernel.Results;

namespace BSX.Modules.Sales.Domain.Errors;

/// <summary>
/// Stable, namespaced error catalog for the Sales domain (ADR-011).
/// Codes never change once published.
/// </summary>
public static class SalesErrors
{
    /// <summary>The lead title is invalid.</summary>
    public static Error LeadTitleInvalid => Error.Validation("SALES.LeadTitle.Invalid", "The lead title is invalid.");

    /// <summary>The lead notes are invalid.</summary>
    public static Error LeadNotesInvalid => Error.Validation(
        "SALES.Lead.NotesInvalid",
        "The lead notes exceed the maximum supported length.");

    /// <summary>The lead has already been marked as contacted.</summary>
    public static Error LeadAlreadyContacted => Error.Conflict(
        "SALES.Lead.AlreadyContacted",
        "The lead has already been marked as contacted.");

    /// <summary>The lead cannot be marked as contacted in its current status.</summary>
    public static Error LeadCannotBeContacted => Error.Conflict(
        "SALES.Lead.CannotBeContacted",
        "Only a new lead can be marked as contacted.");

    /// <summary>The lead must be contacted before it can be qualified.</summary>
    public static Error LeadMustBeContacted => Error.Conflict(
        "SALES.Lead.MustBeContacted",
        "The lead must be contacted before it can be qualified.");

    /// <summary>The lead has already been qualified.</summary>
    public static Error LeadAlreadyQualified => Error.Conflict(
        "SALES.Lead.AlreadyQualified",
        "The lead has already been qualified.");

    /// <summary>A disqualified lead cannot be qualified.</summary>
    public static Error LeadCannotBeQualified => Error.Conflict(
        "SALES.Lead.CannotBeQualified",
        "A disqualified lead cannot be qualified.");

    /// <summary>The lead has already been disqualified.</summary>
    public static Error LeadAlreadyDisqualified => Error.Conflict(
        "SALES.Lead.AlreadyDisqualified",
        "The lead has already been disqualified.");

    /// <summary>A qualified lead cannot be disqualified.</summary>
    public static Error LeadCannotBeDisqualified => Error.Conflict(
        "SALES.Lead.CannotBeDisqualified",
        "A qualified lead cannot be disqualified.");

    /// <summary>The lead can no longer be edited because it is qualified or disqualified.</summary>
    public static Error LeadNotEditable => Error.Conflict(
        "SALES.Lead.NotEditable",
        "Only a new or contacted lead can be edited.");

    /// <summary>The opportunity title is invalid.</summary>
    public static Error OpportunityTitleInvalid => Error.Validation(
        "SALES.OpportunityTitle.Invalid",
        "The opportunity title is invalid.");

    /// <summary>A lead can only become an opportunity when it is qualified.</summary>
    public static Error OpportunityLeadNotQualified => Error.Conflict(
        "SALES.Opportunity.LeadNotQualified",
        "An opportunity can only be created from a qualified lead.");

    /// <summary>The lead and the opportunity must belong to the same tenant.</summary>
    public static Error OpportunityTenantMismatch => Error.Conflict(
        "SALES.Opportunity.TenantMismatch",
        "The lead and the opportunity must belong to the same tenant.");

    /// <summary>The opportunity is won or lost and can no longer be changed.</summary>
    public static Error OpportunityTerminal => Error.Conflict(
        "SALES.Opportunity.Terminal",
        "The opportunity is won or lost and can no longer be changed.");

    /// <summary>The requested stage transition is not valid.</summary>
    public static Error OpportunityInvalidStageTransition => Error.Conflict(
        "SALES.Opportunity.InvalidStageTransition",
        "The requested stage transition is not valid.");

    /// <summary>A negotiation cannot be advanced further; it must be marked won or lost.</summary>
    public static Error OpportunityCannotAdvance => Error.Conflict(
        "SALES.Opportunity.CannotAdvance",
        "A negotiation must be marked won or lost.");

    /// <summary>The monetary amount is invalid.</summary>
    public static Error MoneyAmountInvalid => Error.Validation(
        "SALES.Money.AmountInvalid",
        "The amount must be non-negative, within range and have at most two decimal places.");

    /// <summary>The currency code is invalid.</summary>
    public static Error MoneyCurrencyInvalid => Error.Validation(
        "SALES.Money.CurrencyInvalid",
        "The currency must be a three-letter ISO 4217 code.");

    /// <summary>The probability is invalid.</summary>
    public static Error ProbabilityInvalid => Error.Validation(
        "SALES.Probability.Invalid",
        "The probability must be between 0 and 100.");

    /// <summary>The expected close date is invalid.</summary>
    public static Error ExpectedCloseDateInvalid => Error.Validation(
        "SALES.ExpectedCloseDate.Invalid",
        "The expected close date must be today or later, within the forecasting horizon.");
}
