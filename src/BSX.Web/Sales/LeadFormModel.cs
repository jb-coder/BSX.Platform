using System.ComponentModel.DataAnnotations;

namespace BSX.Web.Sales;

/// <summary>Form model for creating and editing a lead.</summary>
public sealed class LeadFormModel
{
    /// <summary>The lead title.</summary>
    [Required(ErrorMessage = "Lead title is required.")]
    [StringLength(200, ErrorMessage = "Lead title must be 200 characters or fewer.")]
    public string Title { get; set; } = string.Empty;

    /// <summary>The optional CRM customer reference.</summary>
    public Guid? CustomerId { get; set; }

    /// <summary>The optional CRM organization reference.</summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>The optional notes.</summary>
    [StringLength(2000, ErrorMessage = "Notes must be 2,000 characters or fewer.")]
    public string? Notes { get; set; }
}
