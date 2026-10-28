using System.ComponentModel.DataAnnotations;

namespace BSX.Web.Sales;

/// <summary>Form model for creating and editing an opportunity's details and forecast.</summary>
public sealed class OpportunityFormModel : IValidatableObject
{
    /// <summary>The opportunity title.</summary>
    [Required(ErrorMessage = "Opportunity title is required.")]
    [StringLength(200, ErrorMessage = "Opportunity title must be 200 characters or fewer.")]
    public string Title { get; set; } = string.Empty;

    /// <summary>The required CRM customer reference.</summary>
    public Guid? CustomerId { get; set; }

    /// <summary>The optional CRM organization reference.</summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>The potential revenue amount.</summary>
    [Range(typeof(decimal), "0", "1000000000000", ErrorMessage = "Amount must be between 0 and 1,000,000,000,000.")]
    public decimal Amount { get; set; }

    /// <summary>The three-letter ISO currency code.</summary>
    [Required(ErrorMessage = "Currency is required.")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be a three-letter code.")]
    [RegularExpression("^[A-Za-z]{3}$", ErrorMessage = "Use a three-letter ISO currency code, such as USD.")]
    public string Currency { get; set; } = "USD";

    /// <summary>The probability of winning, from 0 to 100.</summary>
    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    public int Probability { get; set; } = 25;

    /// <summary>The expected close date.</summary>
    public DateOnly ExpectedCloseDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(30));

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CustomerId is null || CustomerId == Guid.Empty)
        {
            yield return new ValidationResult(
                "Select a customer for the opportunity.",
                [nameof(CustomerId)]);
        }

        if (decimal.Round(Amount, 2) != Amount)
        {
            yield return new ValidationResult(
                "Amount must have at most two decimal places.",
                [nameof(Amount)]);
        }

        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        if (ExpectedCloseDate == default
            || ExpectedCloseDate < today
            || ExpectedCloseDate > today.AddYears(10))
        {
            yield return new ValidationResult(
                "Expected close date must be today or later, within 10 years.",
                [nameof(ExpectedCloseDate)]);
        }
    }
}
