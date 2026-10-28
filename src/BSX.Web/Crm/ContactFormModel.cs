using System.ComponentModel.DataAnnotations;

namespace BSX.Web.Crm;

/// <summary>Form model for adding and editing a customer contact.</summary>
public sealed class ContactFormModel : IValidatableObject
{
    /// <summary>The contact person's name.</summary>
    [Required(ErrorMessage = "Contact name is required.")]
    [StringLength(200, ErrorMessage = "Contact name must be 200 characters or fewer.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>The optional email address.</summary>
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(320, ErrorMessage = "Email must be 320 characters or fewer.")]
    public string? Email { get; set; }

    /// <summary>The optional phone number.</summary>
    [StringLength(32, ErrorMessage = "Phone number must be 32 characters or fewer.")]
    [RegularExpression(
        "^[0-9+()\\-. ]*$",
        ErrorMessage = "Use digits, spaces and the characters + ( ) - .")]
    public string? PhoneNumber { get; set; }

    /// <summary>Whether the new contact should become the primary contact.</summary>
    public bool IsPrimary { get; set; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        bool hasEmail = !string.IsNullOrWhiteSpace(Email);
        bool hasPhone = !string.IsNullOrWhiteSpace(PhoneNumber);

        if (!hasEmail && !hasPhone)
        {
            yield return new ValidationResult(
                "Provide an email address or a phone number.",
                [nameof(Email), nameof(PhoneNumber)]);
        }

        if (hasPhone && PhoneNumber!.Count(char.IsDigit) < 5)
        {
            yield return new ValidationResult(
                "Phone number must contain at least 5 digits.",
                [nameof(PhoneNumber)]);
        }
    }
}
