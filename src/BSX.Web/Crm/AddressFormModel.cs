using System.ComponentModel.DataAnnotations;
using BSX.Modules.Crm.Application.Common;
using BSX.Modules.Crm.Application.Dtos;

namespace BSX.Web.Crm;

/// <summary>Form model for adding and editing a customer address.</summary>
public sealed class AddressFormModel : IValidatableObject
{
    /// <summary>The address purpose: Billing, Shipping or Other.</summary>
    [Required(ErrorMessage = "Address type is required.")]
    [RegularExpression(
        "^(Billing|Shipping|Other)$",
        ErrorMessage = "Choose Billing, Shipping or Other.")]
    public string Type { get; set; } = nameof(CrmAddressType.Billing);

    /// <summary>The first street line.</summary>
    [Required(ErrorMessage = "Address line 1 is required.")]
    [StringLength(200, ErrorMessage = "Address line 1 must be 200 characters or fewer.")]
    public string Line1 { get; set; } = string.Empty;

    /// <summary>The optional second street line.</summary>
    [StringLength(200, ErrorMessage = "Address line 2 must be 200 characters or fewer.")]
    public string? Line2 { get; set; }

    /// <summary>The city.</summary>
    [Required(ErrorMessage = "City is required.")]
    [StringLength(100, ErrorMessage = "City must be 100 characters or fewer.")]
    public string City { get; set; } = string.Empty;

    /// <summary>The optional region or state.</summary>
    [StringLength(100, ErrorMessage = "Region must be 100 characters or fewer.")]
    public string? Region { get; set; }

    /// <summary>The optional postal code.</summary>
    [StringLength(20, ErrorMessage = "Postal code must be 20 characters or fewer.")]
    public string? PostalCode { get; set; }

    /// <summary>The country.</summary>
    [Required(ErrorMessage = "Country is required.")]
    [StringLength(100, ErrorMessage = "Country must be 100 characters or fewer.")]
    public string Country { get; set; } = string.Empty;

    /// <summary>Builds the address input for the application command.</summary>
    public AddressInput ToAddressInput()
        => new(Line1, Line2, City, Region, PostalCode, Country);

    /// <summary>Parses the selected address type.</summary>
    public bool TryGetAddressType(out CrmAddressType type)
        => Enum.TryParse(Type, ignoreCase: true, out type) && Enum.IsDefined(type);

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!TryGetAddressType(out _))
        {
            yield return new ValidationResult(
                "Choose Billing, Shipping or Other.",
                [nameof(Type)]);
        }
    }
}
