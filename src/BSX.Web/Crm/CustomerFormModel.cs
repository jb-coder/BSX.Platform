using System.ComponentModel.DataAnnotations;

namespace BSX.Web.Crm;

/// <summary>Form model for creating and editing a customer.</summary>
public sealed class CustomerFormModel
{
    /// <summary>The immutable customer code.</summary>
    [Required(ErrorMessage = "Customer code is required.")]
    [StringLength(32, ErrorMessage = "Customer code must be 32 characters or fewer.")]
    [RegularExpression(
        "^[A-Za-z0-9._-]+$",
        ErrorMessage = "Use letters, numbers, hyphens, underscores or periods.")]
    public string Code { get; set; } = string.Empty;

    /// <summary>The customer display name.</summary>
    [Required(ErrorMessage = "Customer name is required.")]
    [StringLength(200, ErrorMessage = "Customer name must be 200 characters or fewer.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>The optional tax identifier.</summary>
    [StringLength(50, ErrorMessage = "Tax identifier must be 50 characters or fewer.")]
    [RegularExpression(
        "^[A-Za-z0-9./-]*$",
        ErrorMessage = "Use letters, numbers, periods, slashes or hyphens.")]
    public string? TaxIdentifier { get; set; }
}
