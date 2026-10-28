using System.ComponentModel.DataAnnotations;
using BSX.Modules.Crm.Application.Dtos;

namespace BSX.Web.Crm;

/// <summary>Form model for creating and editing an organization and its optional primary address.</summary>
public sealed class OrganizationFormModel : IValidatableObject
{
    /// <summary>The organization name.</summary>
    [Required(ErrorMessage = "Organization name is required.")]
    [StringLength(200, ErrorMessage = "Organization name must be 200 characters or fewer.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>The optional tax identifier.</summary>
    [StringLength(50, ErrorMessage = "Tax identifier must be 50 characters or fewer.")]
    [RegularExpression(
        "^[A-Za-z0-9./-]*$",
        ErrorMessage = "Use letters, numbers, periods, slashes or hyphens.")]
    public string? TaxIdentifier { get; set; }

    /// <summary>Whether the optional primary address is included.</summary>
    public bool IncludeAddress { get; set; }

    /// <summary>The first street line.</summary>
    [StringLength(200, ErrorMessage = "Address line 1 must be 200 characters or fewer.")]
    public string? AddressLine1 { get; set; }

    /// <summary>The optional second street line.</summary>
    [StringLength(200, ErrorMessage = "Address line 2 must be 200 characters or fewer.")]
    public string? AddressLine2 { get; set; }

    /// <summary>The city.</summary>
    [StringLength(100, ErrorMessage = "City must be 100 characters or fewer.")]
    public string? City { get; set; }

    /// <summary>The optional region or state.</summary>
    [StringLength(100, ErrorMessage = "Region must be 100 characters or fewer.")]
    public string? Region { get; set; }

    /// <summary>The optional postal code.</summary>
    [StringLength(20, ErrorMessage = "Postal code must be 20 characters or fewer.")]
    public string? PostalCode { get; set; }

    /// <summary>The country.</summary>
    [StringLength(100, ErrorMessage = "Country must be 100 characters or fewer.")]
    public string? Country { get; set; }

    /// <summary>Builds the optional address input.</summary>
    public AddressInput? ToAddressInput()
        => IncludeAddress
            ? new AddressInput(
                AddressLine1 ?? string.Empty,
                AddressLine2,
                City ?? string.Empty,
                Region,
                PostalCode,
                Country ?? string.Empty)
            : null;

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!IncludeAddress)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(AddressLine1))
        {
            yield return new ValidationResult("Address line 1 is required.", [nameof(AddressLine1)]);
        }

        if (string.IsNullOrWhiteSpace(City))
        {
            yield return new ValidationResult("City is required.", [nameof(City)]);
        }

        if (string.IsNullOrWhiteSpace(Country))
        {
            yield return new ValidationResult("Country is required.", [nameof(Country)]);
        }
    }
}
